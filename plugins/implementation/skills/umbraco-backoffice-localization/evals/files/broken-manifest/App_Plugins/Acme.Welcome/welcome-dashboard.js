import { html } from '@umbraco-cms/backoffice/external/lit';
import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';

export class AcmeWelcomeDashboardElement extends UmbLitElement {
    render() {
        return html`
            <uui-box headline=${this.localize.term('welcomeDashboard_headline')}>
                <p>${this.localize.term('welcomeDashboard_intro')}</p>
            </uui-box>
        `;
    }
}

customElements.define('acme-welcome-dashboard', AcmeWelcomeDashboardElement);
export default AcmeWelcomeDashboardElement;
