import { AbstractControl, ValidationErrors } from "@angular/forms";

/**
 * Requires the password and its confirmation to be equal.
 */
export function passwordsMatch(control: AbstractControl): ValidationErrors | null {
    const password = control.get("password")?.value;
    const confirm = control.get("confirm")?.value;
    return password === confirm ? null : { mismatch: true };
}
