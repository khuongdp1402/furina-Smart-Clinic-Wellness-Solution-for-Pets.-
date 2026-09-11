import { test, expect } from "@playwright/test";

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
