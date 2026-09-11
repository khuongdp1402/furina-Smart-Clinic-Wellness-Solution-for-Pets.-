import type { NextConfig } from "next";
import path from "node:path";

const nextConfig: NextConfig = {
  turbopack: {
    // globals.css does `@import "../../design-system/tokens.css"` — by
    // default Turbopack treats this app's own directory as the filesystem
    // root and refuses any path that escapes it ("leaves the filesystem
    // root"). Pointing root at the shared `web/` directory (parent of both
    // `landing` and `design-system`) is Next's documented fix for a
    // monorepo-style shared-file import like this one.
    root: path.join(__dirname, ".."),
  },
};

export default nextConfig;
