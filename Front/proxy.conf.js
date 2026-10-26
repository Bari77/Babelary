module.exports = {
    "/api": {
        target: "http://127.0.0.1:8080",
        secure: false,
    },
    // A route such as /projet/kert-front/editeur/de-DE/home.json looks like a file.
    // Serve the application instead of answering "Cannot GET".
    "^/projet/.*\\..+": {
        target: "http://127.0.0.1:4200",
        bypass() {
            return "/index.html";
        },
    },
};
