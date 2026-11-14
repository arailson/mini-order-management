import { expect, test } from '@playwright/test';

test.describe('Criação de pedido', () => {
  test('cria pedido e aparece na listagem filtrada', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Pedidos' })).toBeVisible();
    await page.getByRole('button', { name: 'Novo pedido' }).click();

    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible();

    await dialog.getByLabel('Cliente').selectOption({ label: 'Pedro Santos' });

    const firstProduto = dialog.locator('select[id^="produto-"]').first();
    const options = firstProduto.locator('option');
    const count = await options.count();
    let chosenValue = '';
    for (let i = 0; i < count; i++) {
      const text = (await options.nth(i).textContent()) ?? '';
      const value = await options.nth(i).getAttribute('value');
      if (value && /estoque:\s*([1-9]\d*)/.test(text)) {
        chosenValue = value;
        break;
      }
    }
    expect(chosenValue, 'deve existir produto com estoque > 0').not.toBe('');
    await firstProduto.selectOption(chosenValue);
    await dialog.locator('input[id^="qtd-"]').first().fill('1');

    const responsePromise = page.waitForResponse(
      (res) => res.url().includes('/pedidos') && res.request().method() === 'POST',
    );
    await dialog.getByRole('button', { name: 'Confirmar pedido' }).click();
    const response = await responsePromise;
    expect(response.status()).toBe(201);
    const body = (await response.json()) as { id: number; valorTotal: number };
    expect(body.id).toBeGreaterThan(0);

    await expect(page.getByText(`Pedido #${body.id} criado com sucesso.`)).toBeVisible();
    await expect(dialog).toHaveCount(0);

    await page.locator('#filtro-cliente').selectOption({ label: 'Pedro Santos' });
    await page.getByRole('button', { name: 'Filtrar' }).click();

    const row = page.locator('table tbody tr', { hasText: String(body.id) });
    await expect(row).toBeVisible();
    await expect(row).toContainText('Pedro Santos');
  });
});
