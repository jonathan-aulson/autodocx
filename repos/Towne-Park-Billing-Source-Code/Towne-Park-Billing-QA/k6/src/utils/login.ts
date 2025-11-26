import puppeteer from 'puppeteer';
import fs from 'fs';
import path from 'path';
import dotenv from 'dotenv';

dotenv.config();

const sleep = (ms: number) => new Promise(res => setTimeout(res, ms));

(async () => {
  const browser = await puppeteer.launch({ headless: false });
  const page = await browser.newPage();

  await page.goto(process.env.BASE_URL || 'https://ambitious-grass-00554670f-develop.eastus2.5.azurestaticapps.net');
  await page.click('[data-qa-id="button-signInWithMicrosoft"]');

  await page.waitForNavigation({ waitUntil: 'networkidle0' });
  await page.type('#i0116', process.env.LOGIN_EMAIL || '');
  await page.click('#idSIButton9');

  await sleep(2000);
  await page.type('#i0118', process.env.LOGIN_PASSWORD || '');
  await page.click('#idSIButton9');

  await page.waitForNavigation({ waitUntil: 'networkidle0' });
  await sleep(2000);
  await page.click('#idSIButton9');
  await page.waitForNavigation({ waitUntil: 'networkidle0' });

  const cookies = await page.cookies();
  const cookieHeader = cookies.map(c => `${c.name}=${c.value}`).join('; ');

  // ✅ Write to actual JS file directly
  const headerJS = `export const headersWithCookie = {
  headers: {
    'Cookie': \`${cookieHeader}\`
  }
};`;

  fs.writeFileSync(path.resolve(__dirname, './cookie.js'), headerJS);
  console.log('✅ headersWithCookie written to cookie.js');

  await browser.close();
})();
