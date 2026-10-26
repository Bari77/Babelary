import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { SessionStore } from "@features/session/stores/session.store";

/**
 * Sends an empty instance to installation, a visitor to login, and a session to the app.
 */
export const sessionGuard: CanActivateFn = async (route) => {
    const store = inject(SessionStore);
    const router = inject(Router);
    await store.ensureReady();

    const path = route.routeConfig?.path ?? "";
    const installed = store.installed() === true;
    const account = store.account();

    if (!installed) {
        return path === "installation" ? true : router.createUrlTree(["/installation"]);
    }

    if (path === "installation") {
        return router.createUrlTree([account ? "/" : "/connexion"]);
    }

    if (!account) {
        return path === "connexion" || path === "rejoindre" ? true : router.createUrlTree(["/connexion"]);
    }

    if (path === "connexion") {
        return router.createUrlTree(["/"]);
    }

    return true;
};
