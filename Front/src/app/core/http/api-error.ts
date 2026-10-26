import { HttpErrorResponse } from "@angular/common/http";

/**
 * Reads the translated API message when the body carries one.
 */
export function apiErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse && typeof error.error?.Message === "string") {
        return error.error.Message;
    }

    return "";
}
