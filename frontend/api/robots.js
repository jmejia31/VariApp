const { publicOrigin, isIndexableHost } = require('../server/seo-utils');

module.exports = async function handler(req, res) {
  const indexable = isIndexableHost(req);
  const origin = publicOrigin(req);

  const body = indexable
    ? [
        'User-agent: *',
        'Disallow: /',
        'Allow: /$',
        'Allow: /varistorehn$',
        'Allow: /varistorehn/',
        'Disallow: /varistorehn/carrito',
        'Disallow: /varistorehn/checkout',
        'Disallow: /varistorehn/cuenta',
        'Disallow: /varistorehn/pedido/',
        `Sitemap: ${origin}/sitemap.xml`,
        ''
      ].join('\n')
    : ['User-agent: *', 'Disallow: /', ''].join('\n');

  res.statusCode = 200;
  res.setHeader('Content-Type', 'text/plain; charset=utf-8');
  res.setHeader('Cache-Control', indexable ? 'public, s-maxage=3600, stale-while-revalidate=86400' : 'no-store');
  res.setHeader('X-Robots-Tag', 'noindex');
  res.end(body);
};
