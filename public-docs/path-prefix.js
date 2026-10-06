export const pathPrefix =
    process.env.ELEVENTY_ENV === "production"
        ? "/single-unique-identifier/"
        : "/";
