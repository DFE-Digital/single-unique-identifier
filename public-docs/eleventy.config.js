import { govukEleventyPlugin } from '@x-govuk/govuk-eleventy-plugin'
import { pathPrefix } from "./path-prefix.js";

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
      navigation: [
        {
          href: '/guides/adding-content/',
          text: 'Adding content'
        },
        {
          href: '/guides/example-integration-guide/',
          text: 'Example integration guide'
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
