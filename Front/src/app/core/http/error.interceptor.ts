import { HttpErrorResponse, HttpInterceptorFn } from "@angular/common/http";
import { inject } from "@angular/core";
import { TranslateService } from "@ngx-translate/core";
import { catchError, throwError } from "rxjs";

/**
 * Translates an API body whose Message is an ERR_* code.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
    const translate = inject(TranslateService);

    return next(req).pipe(
        catchError((err: HttpErrorResponse) => {
            const message = err.error?.Message;
            if (typeof message === "string" && message.startsWith("ERR_")) {
                const detail = typeof err.error?.Detail === "string" ? err.error.Detail : "";
                err.error.Message = translate.instant("ERROR." + message, { token: detail });
            }

            return throwError(() => err);
        }),
    );
};
