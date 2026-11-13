/*
 * Hex colour editor for string properties with UIHint "HexColor" (HexColorEditorDescriptor).
 * Native <input type="color"> plus a text box for typing/pasting a #RRGGBB value.
 * An empty text box clears the value. Server-side the property is validated with a regex as well.
 */
define([
    'dojo/on',
    'dojo/_base/declare',
    'dojo/_base/lang',
    'dijit/_CssStateMixin',
    'dijit/_Widget',
    'dijit/_TemplatedMixin',
    'epi/epi',
    'epi/shell/widget/_ValueRequiredMixin'
],
    function (
        on,
        declare,
        lang,
        _CssStateMixin,
        _Widget,
        _TemplatedMixin,
        epi,
        _ValueRequiredMixin
    ) {
        const HEX_COLOR = /^#[0-9A-Fa-f]{6}$/

        return declare('foundation.editors.HexColorPicker',
            [_Widget, _TemplatedMixin, _CssStateMixin, _ValueRequiredMixin],
            {
                templateString:
                    '<div class="dijitInline">' +
                    '<input type="color" data-dojo-attach-point="colorNode" aria-label="Pick a colour" ' +
                    'style="width:48px;height:28px;padding:0;vertical-align:middle;cursor:pointer;" /> ' +
                    '<input type="text" data-dojo-attach-point="textNode" maxlength="7" placeholder="#RRGGBB" ' +
                    'aria-label="Colour as hex value, for example #1A2B3C" style="width:90px;vertical-align:middle;" />' +
                    '</div>',
                value: null,
                intermediateChanges: false,

                postCreate() {
                    this.inherited(arguments)

                    this.own(
                        // live preview while dragging; commit on change
                        on(this.colorNode, 'input', lang.hitch(this, function () {
                            this.textNode.value = this.colorNode.value
                        })),
                        on(this.colorNode, 'change', lang.hitch(this, function () {
                            this._setValue(this.colorNode.value)
                        })),
                        on(this.textNode, 'change', lang.hitch(this, function () {
                            const typed = this.textNode.value.trim()
                            this._setValue(typed === '' ? null : typed)
                            this._render()
                        }))
                    )

                    this._render()
                },

                isValid() {
                    if (!this.value) {
                        return !this.required
                    }

                    return HEX_COLOR.test(this.value)
                },

                _render() {
                    if (!this.textNode || !this.colorNode) {
                        return
                    }

                    this.textNode.value = this.value || ''

                    if (this.value && HEX_COLOR.test(this.value)) {
                        this.colorNode.value = this.value.toLowerCase()
                    }
                },

                _setValue(value) {
                    if (this._started && epi.areEqual(this.value, value)) {
                        return
                    }

                    this._set('value', value)

                    if (this._started && this.validate()) {
                        this.onChange(value)
                    }
                },

                onChange() {
                    // Event
                },

                _setValueAttr(value) {
                    this._set('value', value || null)
                    this._render()
                },

                _setReadOnlyAttr(value) {
                    this._set('readOnly', value)

                    if (this.colorNode && this.textNode) {
                        this.colorNode.disabled = value
                        this.textNode.readOnly = value
                    }
                },

                _setIntermediateChangesAttr(value) {
                    this._set('intermediateChanges', value)
                }
            }
        )
    })
