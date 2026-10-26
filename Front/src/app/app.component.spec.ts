import { provideHttpClient } from "@angular/common/http";
import { provideRouter } from "@angular/router";
import { TestBed } from "@angular/core/testing";
import { AppComponent } from "./app.component";

describe("AppComponent", () => {
    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [AppComponent],
            providers: [provideRouter([]), provideHttpClient()],
        }).compileComponents();
    });

    it("creates the shell", () => {
        const fixture = TestBed.createComponent(AppComponent);
        expect(fixture.componentInstance).toBeTruthy();
    });
});
