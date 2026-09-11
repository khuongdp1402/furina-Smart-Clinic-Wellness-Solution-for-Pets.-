import { test, expect } from "@playwright/test";

test.describe("Design tokens actually resolve (US-38 AC-1, regression)", () => {
  // A real bug slipped past every other check in this file: Tailwind v4's
  // `@theme inline` silently invalidated every color/font token (CSS
  // custom-property self-reference cycle), and separately next/font's
  // real Fraunces value lost a cascade tie to tokens.css's own fallback
  // declaration. `getByRole`/`toBeVisible` assertions don't notice either
  // one — the elements are still there, just unstyled. Only checking the
  // actual computed value (found by inspecting getComputedStyle in a
  // browser) catches it.
  test("body background and heading font resolve to real values, not empty/fallback", async ({
    page,
  }) => {
    await page.goto("/");

    const bodyBg = await page.evaluate(
      () => getComputedStyle(document.body).backgroundColor
    );
    expect(bodyBg).toBe("rgb(250, 249, 246)"); // --color-surface, #FAF9F6

    const h1Font = await page
      .locator("h1")
      .first()
      .evaluate((el) => getComputedStyle(el).fontFamily);
    expect(h1Font).toContain("Fraunces");
    expect(h1Font).not.toContain("ui-serif"); // the tokens.css fallback stack

    const heroCtaBg = await page
      .locator('section[aria-label="Hero"] button', { hasText: "Đặt lịch ngay" })
      .evaluate((el) => getComputedStyle(el).backgroundColor);
    expect(heroCtaBg).toBe("rgb(249, 112, 102)"); // --color-accent, #F97066
  });
});

test.describe("Landing page (US-38 AC-2)", () => {
  test("renders exactly 8 sections in the spec's order", async ({ page }) => {
    const consoleErrors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });

    await page.goto("/");

    const expectedOrder = [
      "Hero",
      "Vấn đề và giải pháp",
      "Tính năng nổi bật",
      "Bản đồ tìm phòng khám gần nhất",
      "Furina Feed",
      "Dành cho phòng khám",
      "Testimonial và case study",
      "CTA cuối trang và footer",
    ];

    const sections = page.locator("section[aria-label], footer[aria-label]");
    await expect(sections).toHaveCount(expectedOrder.length);

    for (let i = 0; i < expectedOrder.length; i++) {
      await expect(sections.nth(i)).toHaveAttribute(
        "aria-label",
        expectedOrder[i]
      );
    }

    expect(consoleErrors).toEqual([]);
  });
});

test.describe("Design token preview (US-38 AC-1)", () => {
  test("renders every token-driven component without a console error", async ({
    page,
  }) => {
    const consoleErrors: string[] = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });

    await page.goto("/design-preview");

    await expect(page.getByRole("heading", { name: "Design token preview" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Primary" })).toBeVisible();
    await expect(page.getByRole("button", { name: "Accent CTA" })).toBeVisible();
    await expect(page.getByText("Highlight")).toBeVisible();
    await expect(page.getByPlaceholder("Nhập email...")).toBeVisible();

    expect(consoleErrors).toEqual([]);
  });
});
