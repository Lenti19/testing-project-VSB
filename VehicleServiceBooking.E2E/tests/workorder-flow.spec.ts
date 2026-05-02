import { test, expect } from '@playwright/test';

test.describe('WorkOrder Flow (Manager + Mechanic)', () => {

  test('Manager assigns mechanic → WorkOrder auto created', async ({ page }) => {
    await page.goto('/login');

    // Login si Manager
    await page.fill('#email', 'manager@test.com');
    await page.fill('#password', '123456');
    await page.click('button[type="submit"]');

    // Shko te bookings
    await page.goto('/bookings');

    // Zgjedh booking dhe cakto mekanik
    await page.click('[data-test="assign-mechanic-btn"]');
    await page.selectOption('#mechanicSelect', { label: 'Mechanic 1' });
    await page.click('[data-test="confirm-assign"]');

    // Verifiko që WorkOrder u krijua
    await expect(page.locator('[data-test="workorder-row"]')).toBeVisible();
  });

  test('Mechanic starts and completes WorkOrder', async ({ page }) => {
    await page.goto('/login');

    // Login si Mechanic
    await page.fill('#email', 'mechanic@test.com');
    await page.fill('#password', '123456');
    await page.click('button[type="submit"]');

    // Hap WorkOrder
    await page.goto('/workorders');

    await page.click('[data-test="workorder-row"]');

    // Start Work
    await page.click('[data-test="start-work-btn"]');
    await expect(page.locator('[data-test="status"]')).toHaveText('InProgress');

    // Complete Work
    await page.click('[data-test="complete-work-btn"]');

    // Verifiko LaborCost
    await expect(page.locator('[data-test="labor-cost"]')).toBeVisible();
  });

});