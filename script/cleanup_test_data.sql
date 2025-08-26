-- =====================================================================
-- 论坛系统测试数据清理脚本
-- 
-- 描述：清理所有测试数据，保留系统默认数据
-- 版本：1.0
-- 创建日期：2025-08-25
-- 
-- ⚠️  警告：此脚本会删除所有用户数据，仅在开发/测试环境使用！
-- =====================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET AUTOCOMMIT = 0;

START TRANSACTION;

-- =====================================================================
-- 显示清理前的数据统计
-- =====================================================================

SELECT '📊 清理前数据统计：' as Status;

SELECT 'Users:' as Table_Name, COUNT(*) as Count FROM users
UNION ALL
SELECT 'Topics:' as Table_Name, COUNT(*) as Count FROM topics  
UNION ALL
SELECT 'Posts:' as Table_Name, COUNT(*) as Count FROM posts
UNION ALL
SELECT 'User Roles:' as Table_Name, COUNT(*) as Count FROM user_roles
UNION ALL
SELECT 'Topic Tags:' as Table_Name, COUNT(*) as Count FROM topic_tags
UNION ALL
SELECT 'Post Mentions:' as Table_Name, COUNT(*) as Count FROM post_mentions
UNION ALL
SELECT 'Email Tokens:' as Table_Name, COUNT(*) as Count FROM email_verification_tokens
UNION ALL
SELECT 'Refresh Tokens:' as Table_Name, COUNT(*) as Count FROM refresh_tokens;

-- =====================================================================
-- 清理用户相关数据（保持关联完整性）
-- =====================================================================

-- 1. 清理帖子提及关系
DELETE FROM post_mentions WHERE post_id > 0;

-- 2. 清理所有帖子
DELETE FROM posts WHERE id > 0;

-- 3. 清理主题标签关系
DELETE FROM topic_tags WHERE topic_id > 0;

-- 4. 清理所有主题
DELETE FROM topics WHERE id > 0;

-- 5. 清理分类版主关系
DELETE FROM category_moderators WHERE user_id > 0;

-- 6. 清理用户角色
DELETE FROM user_roles WHERE user_id > 0;

-- 7. 清理刷新令牌
DELETE FROM refresh_tokens WHERE user_id > 0;

-- 8. 清理邮箱验证令牌
DELETE FROM email_verification_tokens WHERE user_id > 0;

-- 9. 最后清理用户
DELETE FROM users WHERE id > 0;

-- =====================================================================
-- 重置自增ID
-- =====================================================================

ALTER TABLE users AUTO_INCREMENT = 1;
ALTER TABLE topics AUTO_INCREMENT = 1;
ALTER TABLE posts AUTO_INCREMENT = 1;
ALTER TABLE email_verification_tokens AUTO_INCREMENT = 1;
ALTER TABLE refresh_tokens AUTO_INCREMENT = 1;

-- =====================================================================
-- 重置标签使用计数
-- =====================================================================

UPDATE tags SET usage_count = 0;

-- =====================================================================
-- 验证清理结果
-- =====================================================================

SELECT '🧹 清理后数据统计：' as Status;

SELECT 'Users:' as Table_Name, COUNT(*) as Remaining FROM users
UNION ALL
SELECT 'Topics:' as Table_Name, COUNT(*) as Remaining FROM topics  
UNION ALL
SELECT 'Posts:' as Table_Name, COUNT(*) as Remaining FROM posts
UNION ALL
SELECT 'User Roles:' as Table_Name, COUNT(*) as Remaining FROM user_roles
UNION ALL
SELECT 'Topic Tags:' as Table_Name, COUNT(*) as Remaining FROM topic_tags
UNION ALL
SELECT 'Post Mentions:' as Table_Name, COUNT(*) as Remaining FROM post_mentions
UNION ALL
SELECT 'Email Tokens:' as Table_Name, COUNT(*) as Remaining FROM email_verification_tokens
UNION ALL
SELECT 'Refresh Tokens:' as Table_Name, COUNT(*) as Remaining FROM refresh_tokens;

-- =====================================================================
-- 显示保留的系统数据
-- =====================================================================

SELECT '📋 保留的系统数据：' as Status;

SELECT 'Categories:' as Table_Name, COUNT(*) as Count FROM categories
UNION ALL  
SELECT 'Tags:' as Table_Name, COUNT(*) as Count FROM tags;

SELECT '分类列表：' as Info;
SELECT id, name, slug, description FROM categories ORDER BY `order`;

SELECT '标签列表：' as Info;  
SELECT id, name, slug, description, usage_count FROM tags ORDER BY id;

-- =====================================================================
-- 提交清理操作
-- =====================================================================

COMMIT;
SET AUTOCOMMIT = 1;
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- 清理完成提示
-- =====================================================================

SELECT '✅ 测试数据清理完成！' as Status;
SELECT '🔄 数据库已重置为初始状态，保留了系统默认的分类和标签' as Summary;
SELECT '💡 如需重新生成测试数据，请运行 insert_test_data.sql' as Tip;