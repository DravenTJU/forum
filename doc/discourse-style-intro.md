# Discourse 论坛的风格（详解）

**TL;DR**：Discourse 的视觉与交互风格是“内容即界面”。信息密度高但排布极其规整：主题列表用**颜色分类徽章 + 标签**与**头像栈**表达语义；主题详情用**右侧时间轴**与**无限滚动**替代传统分页；底部**浮动编辑器（Composer）**支持即写即见的编辑/预览；链接自动**富预览（Onebox）**；信任等级与**徽章体系**把“治理”融入 UI。

---

## 1) 版面与导航

* **顶部导航条**：左侧主导航（Latest/New/Unread/Top/Categories），右侧全局搜索与用户菜单；移动端会折叠为更紧凑的图标导航。主题列表顶部还可出现**过滤器**与范围切换。近年的“Topic Filter”提供了更强的组合筛选语法（多条件、排序等）。 ([Discourse Meta][1])
* **主题列表卡片**：

  * 标题左侧/下方出现**分类徽章**（带色条/色块）与**标签**；右侧是**回复数/浏览量/最后活跃时间**等弱化信息；行尾展示**最近参与者的头像栈**，形成“谁在讨论”的视觉线索。分类色彩可通过主题组件扩展为更显著的**彩色分类**或**分类横幅**。 ([Discourse Meta][2])
* **用户卡片（User Card）**：点击头像或用户名弹出轻量信息卡，便于就地了解他人资料与动作；管理员可以进一步自定义卡片字段和样式。 ([Discourse Meta][3])

## 2) 主题阅读体验

* **无限滚动**：列表与帖子流默认采用**Infinite Scroll**，随滚随载、无显式页码（JS 关闭时退化为分页）。这让长帖阅读保持连续，不必频繁跳页。 ([Discourse Meta][4])
* **右侧时间轴（Timeline）**：主题详情右侧以**垂直时间轴/滚动条**表达“从首帖到最新帖”的位置感，可拖拽快速跳转、查看总楼层与进度，替代传统分页定位。 ([Discourse Meta][5])
* **锚点与历史**：滚动时 URL 会动态更新到当前楼层锚点（便于分享与返回原位），与无限滚动协同工作（官方工程讨论与博客多有论述）。 ([Discourse Meta][4])

## 3) 创作与互动

* **底部浮动编辑器（Composer）**：在页面底部“抽屉式”出现，默认**编辑/预览并排**，支持切换布局与全屏；移动端布局经过特别优化，也有社区组件提供额外排版按钮。 ([Discourse Meta][6])
* **选中即引用 / 快捷引用**：选中文本即可“引用回复”，提升长帖讨论的可读性与上下文连结。 ([Discourse Meta][7])
* **Onebox 富链接预览**：把 URL 单独一行粘贴即可自动生成**摘要卡片/内嵌媒体**（支持 OpenGraph/oEmbed 与常见站点的特例规则；管理员可配置/屏蔽域名）。 ([Discourse Meta][8])
* **@提及 / 表情 / 书签 / 解决方案**：轻量而一致的微交互；“已解决”可通过官方组件提供显著的**Solved 徽标**样式。 ([Discourse Meta][9])

## 4) 视觉语言

* **层次清晰、留白充足**：标题与元信息（分类、标签、计数、时间）分级排版，颜色只在必要之处使用（分类色、状态徽章），整体以无衬线字体和轻边框/分隔线为主。
* **颜色编码的分类**：分类自带主/副色，出现在列表行、时间轴、Composer 等界面部位以形成一致的“语义色”导引。 ([Discourse Meta][2])
* **组件化可定制**：官方提供**主题/主题组件**机制与 SCSS 变量、图标替换、暗色模式等设计入口；“Designers Guide”详细说明了主题工作流与样式可控点。 ([Discourse Meta][10], [GitHub][11])

## 5) 信息架构与可发现性

* \*\*分类（Category）× 标签（Tag）\*\*的“双维度”组织：类别管权限与大方向，标签精细化主题；分类/标签页可配横幅与说明文，强化入口语义。 ([Discourse Meta][12])
* **高级筛选与搜索**：从顶栏搜索到“Topic Filter”语法，支持多条件组合、排序与保存常用查询，提高老站点内容再发现效率。 ([Discourse Meta][1])

## 6) 社区治理可视化

* **信任等级（TL0–TL4）**与**徽章**：新用户逐步解锁权限，活跃者获得更强的编辑/维护能力；UI 中通过徽章、提示与用户页摘要强化“成长路径”的可感知性。 ([Discourse Meta][13], [maown.com][14], [LINUX DO][15])
* **版主/管理员操作**：置顶、锁定、移动主题等在主题页以按钮与状态样式呈现（被锁定时输入区禁用），治理动作“界面化、低门槛”。

## 7) 移动端体验

* **自适应优先**：移动端保留同样的信息结构与互动能力；可选“底部标签栏”等主题组件强化单手操作；`/new-topic` 路由支持从固定入口直接拉起 Composer。 ([Discourse Meta][16])

## 8) 扩展与主题生态

* 官方与社区维护的**主题组件**覆盖：分类颜色强化、分类/标签页侧边栏、用户目录卡片化等，延续“内容即界面”的基调而不破坏核心信息架构。 ([GitHub][17], [Discourse Meta][18])

---

### 设计小结（借鉴要点）

1. 以**主题列表单元**为信息密度与可扫读性的平衡点（标题 + 分类/标签 + 头像栈 + 计数/时间）。
2. 用**时间轴 + 无限滚动**替代分页，保证**位置感**与**连续性**。 ([Discourse Meta][4])
3. 让“创建与讨论”始终在场：**底部 Composer**不打断阅读流，引用与 Onebox 降低表达成本。 ([Discourse Meta][6])
4. 把“治理”融入 UI：**信任等级/徽章**、锁定/置顶等状态都有明确可视表达。 ([Discourse Meta][13])

[1]: https://meta.discourse.org/t/filtering-topic-lists-in-discourse/375558?utm_source=chatgpt.com "Filtering topic lists in Discourse - Using Discourse - Discourse Meta"
[2]: https://meta.discourse.org/t/colorful-categories/207267?utm_source=chatgpt.com "Colorful Categories - Theme component - Discourse Meta"
[3]: https://meta.discourse.org/t/using-user-cards-to-quickly-view-information-about-others/44093?utm_source=chatgpt.com "Using user cards to quickly view information about others - Discourse Meta"
[4]: https://meta.discourse.org/t/understanding-infinite-scrolling/30804?utm_source=chatgpt.com "Understanding infinite scrolling - Discourse Meta"
[5]: https://meta.discourse.org/t/change-right-gutter-to-vertical-timeline-topic-controls/44096?utm_source=chatgpt.com "Change right gutter to vertical timeline + topic controls"
[6]: https://meta.discourse.org/t/toggle-composer-layout-to-position-editor-and-preview-as-top-bottom-instead-of-left-right/261818?utm_source=chatgpt.com "Toggle Composer layout to position Editor and Preview as top-bottom ..."
[7]: https://meta.discourse.org/t/how-do-i-quote-reply-when-appearance-of-quote-reply-button-on-text-selection-is-turned-off/51761?utm_source=chatgpt.com "How do I quote reply when appearance of \"quote reply ... - Discourse Meta"
[8]: https://meta.discourse.org/t/creating-rich-link-previews-with-onebox/98088?utm_source=chatgpt.com "Creating rich link previews with Onebox - Discourse Meta"
[9]: https://meta.discourse.org/t/solved-topic-badge/281981?utm_source=chatgpt.com "Solved Topic Badge - Theme component - Discourse Meta"
[10]: https://meta.discourse.org/t/designers-guide-to-getting-started-with-themes-in-discourse/152002?utm_source=chatgpt.com "Designer's Guide to getting started with themes in Discourse"
[11]: https://github.com/discourse/discourse-developer-docs/blob/main/docs/05-themes-components/03-designers-guide.md?utm_source=chatgpt.com "discourse-developer-docs/docs/05-themes-components/03-designers-guide ..."
[12]: https://meta.discourse.org/t/category-group-tag-descriptions-as-topics/297605?utm_source=chatgpt.com "Category, Group, Tag Descriptions as Topics - Discourse Meta"
[13]: https://meta.discourse.org/t/understanding-and-using-badges/32540?utm_source=chatgpt.com "Understanding and using badges - Discourse Meta"
[14]: https://maown.com/t/topic/80?utm_source=chatgpt.com "了解论坛的信任等级 - Discourse - 媉"
[15]: https://linux.do/t/topic/2460?utm_source=chatgpt.com "【新人请看】了解Discourse信任度 - 文档共建 - LINUX DO"
[16]: https://meta.discourse.org/t/discourse-tab-bar-for-mobile/75696?utm_source=chatgpt.com "Discourse Tab Bar for Mobile - Theme component - Discourse Meta"
[17]: https://github.com/discourse/discourse-topic-list-sidebars?utm_source=chatgpt.com "Discourse Topic List Sidebars - GitHub"
[18]: https://meta.discourse.org/t/user-card-directory/144479?utm_source=chatgpt.com "User Card Directory - Theme component - Discourse Meta"
