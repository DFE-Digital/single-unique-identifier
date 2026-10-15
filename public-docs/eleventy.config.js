import { govukEleventyPlugin } from '@x-govuk/govuk-eleventy-plugin'
import { pathPrefix } from "./path-prefix.js";

const siteUrl = new URL('https://dfe-digital.github.io/single-unique-identifier/')
const sitePath = siteUrl.pathname.replace(/\/$/, '')

function openExternalLinksInNewTabs(html) {
  return html.replace(/<a\b[^>]*>/gi, (tag) => {
    const href = tag.match(/\s+href\s*=\s*(["'])(.*?)\1/i)?.[2]
    if (!href || !/^https?:\/\//i.test(href)) return tag

    let url
    try {
      url = new URL(href)
    } catch {
      return tag
    }
    if (url.origin === siteUrl.origin &&
        (url.pathname === sitePath || url.pathname.startsWith(`${sitePath}/`))) return tag

    const relMatch = tag.match(/\s+rel\s*=\s*(["'])(.*?)\1/i)
    const rel = new Set((relMatch?.[2] ?? '').split(/\s+/).filter(Boolean))
    rel.add('noopener')
    rel.add('noreferrer')

    tag = relMatch
      ? tag.replace(relMatch[0], ` rel="${[...rel].join(' ')}"`)
      : tag.replace(/>$/, ` rel="${[...rel].join(' ')}">`)

    return /\s+target\s*=\s*(["']).*?\1/i.test(tag)
      ? tag.replace(/\s+target\s*=\s*(["']).*?\1/i, ' target="_blank"')
      : tag.replace(/>$/, ' target="_blank">')
  })
}

export default function(eleventyConfig) {
  eleventyConfig.addPassthroughCopy("content/assets/images");

  eleventyConfig.addPlugin(govukEleventyPlugin, {
    stylesheets: ['/assets/styles.css'],
    titleSuffix: 'Single Unique Identifier',
    templates: {
      sitemap: true,
      searchIndex: true
    },
    header: {
      logotype: {
        html: `<img src="${pathPrefix}assets/images/department-for-education_white.png" alt="Department for Education">`
      },
      search: {
        indexPath: `${pathPrefix}search-index.json`,
        sitemapPath: '/sitemap'
      }
    },
    serviceNavigation: {
      serviceName: 'Single Unique Identifier',
      serviceUrl: pathPrefix,
      navigation: [
        {
          href: '/guides/developer-documentation/',
          text: 'Developer documentation'
        }
      ]
    },
    footer: {
      logo: false,
      meta: {
        items: [
          {
            href: 'https://github.com/DFE-Digital/single-unique-identifier',
            text: 'Single Unique Identifier'
          }
        ]
      }
    }
  })

  eleventyConfig.addTransform('external-links', function (content) {
    return this.outputPath?.endsWith('.html')
      ? openExternalLinksInNewTabs(content)
      : content
  })

  return {
    pathPrefix: pathPrefix,
    dataTemplateEngine: 'njk',
    htmlTemplateEngine: 'njk',
    markdownTemplateEngine: 'njk',
    dir: {
      input: 'content',
    }
  }
};
