import { html, customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

export class AcmeWelcomeDashboardElement extends UmbLitElement {
    render() {
        const pending = 3;
        return html`
            <uui-box headline="Welcome to the Acme editor">
                <p>Start by choosing a page in the content tree.</p>
                <p>You have ${pending} pages waiting for review.</p>
                <uui-button look="primary" label="Open review queue"></uui-button>
            </uui-box>
        `;
    }
}

customElements.define('acme-welcome-dashboard', AcmeWelcomeDashboardElement);
export default AcmeWelcomeDashboardElement;
