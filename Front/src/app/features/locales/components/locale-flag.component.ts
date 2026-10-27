import { Component, computed, input } from "@angular/core";
import { localeFlag } from "@features/locales/models/locale.model";

/**
 * Country flag for an xx-XX locale. Renders nothing when the code has no country.
 */
@Component({
    selector: "app-locale-flag",
    standalone: true,
    template: `
        @if (emoji(); as flag) {
            <span class="locale-flag" aria-hidden="true">{{ flag }}</span>
        }
    `,
    styles: `
        :host {
            display: contents;
        }

        .locale-flag {
            margin-inline-end: 6px;
            font-family: "Twemoji Country Flags", sans-serif;
            font-size: 1.05em;
            line-height: 1;
            vertical-align: -0.12em;
        }
    `,
})
export class LocaleFlagComponent {
    public readonly code = input("");
    public readonly emoji = computed(() => localeFlag(this.code()));
}
