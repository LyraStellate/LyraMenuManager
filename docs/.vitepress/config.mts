import { defineConfig } from 'vitepress'
import { tabsMarkdownPlugin } from 'vitepress-plugin-tabs'

export default defineConfig({
    markdown: {
        config(md) {
            md.use(tabsMarkdownPlugin)
        }
    },
    title: "Lyra Menu Manager",
    description: "VRChat向けアバターメニューを直感的なUIで視覚的に整理・編集できるUnityエディター拡張ツールのドキュメント",
    lang: 'ja',
    base: '/MenuManager/',
    themeConfig: {
        nav: [
            { text: 'ポータルホーム', link: 'https://docs.lyrastellate.dev/' },
            { text: 'ホーム', link: '/' },
            { text: 'ガイド', link: '/guide/' }
        ],

        sidebar: [
            {
                text: 'ガイド',
                items: [
                    { text: '概要', link: '/guide/' },
                    { text: '導入方法', link: '/guide/getting-started' },
                    { text: '基本的な使い方', link: '/guide/how-to-use' },
                    { text: 'コンポーネント', link: '/guide/component' },
                    { text: '詳細', link: '/guide/explanation' },
                    {
                        text: '技術仕様',
                        collapsed: false,
                        items: [
                            { text: 'アーキテクチャ概要', link: '/guide/technical-details/architecture' },
                            { text: 'MenuManager.cs', link: '/guide/technical-details/MenuManager' },
                            { text: 'MenuManagerPlugin.cs', link: '/guide/technical-details/MenuManagerPlugin' },
                            { text: 'MenuLayoutData.cs', link: '/guide/technical-details/MenuLayoutData' },
                            { text: 'MenuLayoutDataEditor.cs', link: '/guide/technical-details/MenuLayoutDataEditor' },
                            { text: 'MenuManagerSettingsWindow.cs', link: '/guide/technical-details/MenuManagerSettingsWindow' }
                        ]
                    },
                    { text: '更新履歴', link: '/guide/changelog' }
                ]
            }
        ],

        socialLinks: [
            { icon: 'github', link: 'https://docs.lyrastellate.dev/MenuManager/' }
        ],

        search: {
            provider: 'local'
        },

        outline: {
            label: '目次'
        },

        docFooter: {
            prev: '前のページ',
            next: '次のページ'
        }
    }
})
