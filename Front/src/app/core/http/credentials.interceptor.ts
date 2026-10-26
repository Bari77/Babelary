import { HttpInterceptorFn } from "@angular/common/http";

/**
 * Sends the session cookie on each API call.
 */
export const credentialsInterceptor: HttpInterceptorFn = (req, next) => {
    return next(req.clone({ withCredentials: true }));
};
