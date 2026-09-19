const {
  apiBase,
  publicOrigin,
  isIndexableHost,
  fetchJson,
  escapeXml,
  safeSlug
} = require('../server/seo-utils');

function xmlUrl(loc, lastmod) {
  const mod = lastmod && Number.isFinite(Date.parse(lastmod))
    ? `<lastmod>${escapeXml(new Date(lastmod).toISOString())}</lastmod>`
    : '';
  return `<url><loc>${escapeXml(loc)}</loc>${mod}</url>`;
}

async function loadCategories(req) {
  const payload = await fetchJson(`${apiBase(req)}/tienda/categorias`);
  const items = payload && payload.success && Array.isArray(payload.data) ? payload.data : [];
  return items.filter(item => safeSlug(item?.slug));
}

async function loadProducts(req) {
  const all = [];
  let page = 1;
  let totalPages = 1;

  while (page <= totalPages && page <= 100) {
    const payload = await fetchJson(`${apiBase(req)}/tienda/productos?page=${page}&pageSize=96`);
    const data = payload && payload.success ? payload.data : null;
    if (!data || !Array.isArray(data.items)) break;

    all.push(...data.items.filter(item => item && item.activo !== false && safeSlug(item.slug)));
    const pageSize = Number(data.pageSize) > 0 ? Number(data.pageSize) : 96;
    const totalCount = Number(data.totalCount) >= 0 ? Number(data.totalCount) : data.items.length;
    totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
    page += 1;
  }

  return all;
}

module.exports = async function handler(req, res) {
  if (!isIndexableHost(req)) {
    res.statusCode = 200;
    res.setHeader('Content-Type', 'application/xml; charset=utf-8');
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('X-Robots-Tag', 'noindex');
    res.end('<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"></urlset>');
    return;
  }

  const origin = publicOrigin(req);
  const urls = new Map();
  for (const path of ['/varistorehn', '/varistorehn/productos', '/varistorehn/ofertas', '/varistorehn/categorias']) {
    urls.set(`${origin}${path}`, '');
  }

  try {
    const [categories, products] = await Promise.all([loadCategories(req), loadProducts(req)]);

    for (const category of categories) {
      urls.set(`${origin}/varistorehn/categoria/${encodeURIComponent(category.slug)}`, '');
    }
    for (const product of products) {
      urls.set(
        `${origin}/varistorehn/producto/${encodeURIComponent(product.slug)}`,
        product.fechaCreacion || ''
      );
    }
  } catch {
    // Las rutas estáticas públicas permanecen disponibles aun si el catálogo está temporalmente caído.
  }

  const body = [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">',
    ...[...urls.entries()].map(([loc, lastmod]) => xmlUrl(loc, lastmod)),
    '</urlset>'
  ].join('');

  res.statusCode = 200;
  res.setHeader('Content-Type', 'application/xml; charset=utf-8');
  res.setHeader('Cache-Control', 'public, s-maxage=3600, stale-while-revalidate=86400');
  res.setHeader('X-Robots-Tag', 'noindex');
  res.end(body);
};
