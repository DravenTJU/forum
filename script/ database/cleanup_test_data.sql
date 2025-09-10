-- =====================================================================
-- Forum System Test Data Cleanup Script
-- 
-- Description: Clean all test data, preserve system default data
-- Version: 1.0
-- Created: 2025-08-25
-- 
-- ⚠️  Warning: This script will delete all user data, use only in development/testing environments!
-- =====================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET AUTOCOMMIT = 0;

START TRANSACTION;

-- =====================================================================
-- Display data statistics before cleanup
-- =====================================================================

SELECT '📊 Data statistics before cleanup:' as Status;

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
-- Clean user-related data (maintain referential integrity)
-- =====================================================================

-- 1. Clean post mention relationships
DELETE FROM post_mentions WHERE post_id > 0;

-- 2. Clean all posts
DELETE FROM posts WHERE id > 0;

-- 3. Clean topic-tag relationships
DELETE FROM topic_tags WHERE topic_id > 0;

-- 4. Clean all topics
DELETE FROM topics WHERE id > 0;

-- 5. Clean category moderator relationships
DELETE FROM category_moderators WHERE user_id > 0;

-- 6. Clean user roles
DELETE FROM user_roles WHERE user_id > 0;

-- 7. Clean refresh tokens
DELETE FROM refresh_tokens WHERE user_id > 0;

-- 8. Clean email verification tokens
DELETE FROM email_verification_tokens WHERE user_id > 0;

-- 9. Finally clean users
DELETE FROM users WHERE id > 0;

-- =====================================================================
-- Reset auto-increment IDs
-- =====================================================================

ALTER TABLE users AUTO_INCREMENT = 1;
ALTER TABLE topics AUTO_INCREMENT = 1;
ALTER TABLE posts AUTO_INCREMENT = 1;
ALTER TABLE email_verification_tokens AUTO_INCREMENT = 1;
ALTER TABLE refresh_tokens AUTO_INCREMENT = 1;

-- =====================================================================
-- Reset tag usage count
-- =====================================================================

UPDATE tags SET usage_count = 0;

-- =====================================================================
-- Verify cleanup results
-- =====================================================================

SELECT '🧹 Data statistics after cleanup:' as Status;

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
-- Display preserved system data
-- =====================================================================

SELECT '📋 Preserved system data:' as Status;

SELECT 'Categories:' as Table_Name, COUNT(*) as Count FROM categories
UNION ALL  
SELECT 'Tags:' as Table_Name, COUNT(*) as Count FROM tags;

SELECT 'Category list:' as Info;
SELECT id, name, slug, description FROM categories ORDER BY `order`;

SELECT 'Tag list:' as Info;  
SELECT id, name, slug, description, usage_count FROM tags ORDER BY id;

-- =====================================================================
-- Commit cleanup operation
-- =====

COMMIT;
SET AUTOCOMMIT = 1;
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- Cleanup completion message
-- =====================================================================

SELECT '✅ Test data cleanup completed!' as Status;
SELECT '🔄 Database has been reset to initial state, preserved system default categories and tags' as Summary;
SELECT '💡 To regenerate test data, please run insert_test_data.sql' as Tip;