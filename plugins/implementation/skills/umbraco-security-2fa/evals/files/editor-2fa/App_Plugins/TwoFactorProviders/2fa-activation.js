// Sam's copy of the docs' custom activation element; unchanged from the docs sample apart from the tag name.
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html } from '@umbraco-cms/backoffice/external/lit';
export default class Acme2faActivationElement extends UmbLitElement {
    render() { return html`<p>See the Umbraco docs sample.</p>`; }
}
customElements.define('acme-2fa-activation', Acme2faActivationElement);
