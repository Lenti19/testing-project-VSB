import { test, expect } from '@playwright/test';

test.describe('Invoice UI Flow', () => {

  test('Generate invoice with correct totals', async ({ page }) => {
    await page.goto('/login');

    // Login si Manager
    await page.fill('#email', 'manager@test.com');
    await page.fill('#password', '123456');
    await page.click('button[type="submit"]');

    // Hap WorkOrder të kompletuar
    await page.goto('/workorders');
    await page.click('[data-test="completed-workorder"]');

    // Gjenero faturë
    await page.click('[data-test="generate-invoice-btn"]');

    // Validime
    const subTotal = await page.locator('[data-test="subtotal"]').innerText();
    const tax = await page.locator('[data-test="tax"]').innerText();
    const total = await page.locator('[data-test="total"]').innerText();

    const sub = parseFloat(subTotal);
    const t = parseFloat(tax);
    const tot = parseFloat(total);

    expect(t).toBeCloseTo(sub * 0.18, 2);
    expect(tot).toBeCloseTo(sub + t, 2);
  });

  test('Prevent duplicate invoice creation', async ({ page }) => {
    await page.goto('/login');

    await page.fill('#email', 'manager@test.com');
    await page.fill('#password', '123456');
    await page.click('button[type="submit"]');

    await page.goto('/workorders');
    await page.click('[data-test="completed-workorder"]');

    // Kliko prap
    await page.click('[data-test="generate-invoice-btn"]');

    // Verifiko error
    await expect(page.locator('[data-test="error-message"]'))
      .toHaveText('Invoice ekziston');
  });

});