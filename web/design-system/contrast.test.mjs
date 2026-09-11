import { test } from "node:test";
import assert from "node:assert/strict";
import { contrastRatio } from "./contrast.mjs";

test("black on white is the maximum ratio, 21:1", () => {
  assert.equal(contrastRatio("#000000", "#FFFFFF").toFixed(2), "21.00");
});

test("ink text on the default surface passes AA comfortably", () => {
  assert.equal(contrastRatio("#292524", "#FAF9F6").toFixed(2), "14.41");
});

test("white text on primary passes AA for normal text (>= 4.5)", () => {
  const ratio = contrastRatio("#FFFFFF", "#0F766E");
  assert.ok(ratio >= 4.5, `expected >= 4.5, got ${ratio}`);
});

test("white text on accent fails AA even for large text (< 3.0)", () => {
  const ratio = contrastRatio("#FFFFFF", "#F97066");
  assert.ok(ratio < 3.0, `expected < 3.0, got ${ratio}`);
});

test("is symmetric regardless of argument order", () => {
  const a = contrastRatio("#292524", "#FAF9F6");
  const b = contrastRatio("#FAF9F6", "#292524");
  assert.equal(a, b);
});
