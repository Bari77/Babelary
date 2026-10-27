import { inject, signal } from "@angular/core";
import { TranslateService } from "@ngx-translate/core";
import { firstValueFrom } from "rxjs";

const storageKey = "babelary.lang";

/** Languages shipped with the site. English is the default. */
export const interfaceLocales = ["en-US", "fr-FR"] as const;

export type InterfaceLocaleCode = (typeof interfaceLocales)[number];

/**
 * Language saved on this browser. An unknown value falls back to English.
 */
export function readInterfaceLocale(): InterfaceLocaleCode {
    try {
        const stored = localStorage.getItem(storageKey);
        if (stored === "fr-FR" || stored === "en-US") {
            return stored;
        }
    } catch {
        // A private window can refuse storage. The site stays in English.
    }

    return "en-US";
}

/**
 * Name of an interface language, written in that language.
 */
export function interfaceLocaleLabel(code: InterfaceLocaleCode): string {
    const languageNames = new Intl.DisplayNames([code], { type: "language" });
    const regionNames = new Intl.DisplayNames([code], { type: "region" });
    const separator = code.indexOf("-");
    const language = languageNames.of(code.slice(0, separator));
    const region = regionNames.of(code.slice(separator + 1));
    const text = `${language} (${region})`;
    return text.charAt(0).toLocaleUpperCase(code) + text.slice(1);
}

/**
 * The language of the interface on this browser.
 */
export class InterfaceLocale {
    public readonly code = signal<InterfaceLocaleCode>(readInterfaceLocale());

    private readonly translate = inject(TranslateService);

    public constructor() {
        document.documentElement.lang = this.code();
    }

    public async choose(code: string): Promise<void> {
        if ((code !== "fr-FR" && code !== "en-US") || code === this.code()) {
            return;
        }

        try {
            localStorage.setItem(storageKey, code);
        } catch {
            // The language still changes for this visit when storage is refused.
        }

        this.code.set(code);
        document.documentElement.lang = code;
        await firstValueFrom(this.translate.use(code));
    }
}
