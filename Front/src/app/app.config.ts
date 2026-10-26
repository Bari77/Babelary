import { registerLocaleData } from "@angular/common";
import { provideHttpClient, withInterceptors } from "@angular/common/http";
import localeFr from "@angular/common/locales/fr";
import { ApplicationConfig, LOCALE_ID, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from "@angular/core";
import { provideRouter } from "@angular/router";
import { credentialsInterceptor } from "@core/http/credentials.interceptor";
import { errorInterceptor } from "@core/http/error.interceptor";
import { HealthService } from "@features/home/services/health.service";
import { HomeStore } from "@features/home/stores/home.store";
import { LocalesService } from "@features/locales/services/locales.service";
import { LocalesStore } from "@features/locales/stores/locales.store";
import { ProfileService } from "@features/profile/services/profile.service";
import { TokensService } from "@features/profile/services/tokens.service";
import { ProfileStore } from "@features/profile/stores/profile.store";
import { ProjectsService } from "@features/projects/services/projects.service";
import { ProjectsStore } from "@features/projects/stores/projects.store";
import { SessionService } from "@features/session/services/session.service";
import { SetupService } from "@features/session/services/setup.service";
import { SessionStore } from "@features/session/stores/session.store";
import { provideTranslateLoader, provideTranslateService } from "@ngx-translate/core";
import { TRANSLATE_HTTP_LOADER_CONFIG, TranslateHttpLoader } from "@ngx-translate/http-loader";
import { routes } from "./app.routes";

registerLocaleData(localeFr);

export const appConfig: ApplicationConfig = {
    providers: [
        provideBrowserGlobalErrorListeners(),
        provideZoneChangeDetection({ eventCoalescing: true }),
        provideRouter(routes),
        provideHttpClient(withInterceptors([credentialsInterceptor, errorInterceptor])),
        { provide: LOCALE_ID, useValue: "fr-FR" },
        SetupService,
        SessionService,
        ProfileService,
        TokensService,
        HealthService,
        LocalesService,
        LocalesStore,
        SessionStore,
        ProfileStore,
        ProjectsService,
        ProjectsStore,
        HomeStore,
        {
            provide: TRANSLATE_HTTP_LOADER_CONFIG,
            useValue: {
                prefix: "/i18n/",
                suffix: ".json",
                useHttpBackend: true,
            },
        },
        provideTranslateService({
            fallbackLang: "fr-FR",
            lang: "fr-FR",
            loader: provideTranslateLoader(TranslateHttpLoader),
        }),
    ],
};
