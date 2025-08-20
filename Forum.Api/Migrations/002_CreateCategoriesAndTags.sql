-- Categories
CREATE TABLE categories (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL,
  slug VARCHAR(100) NOT NULL UNIQUE,
  description TEXT,
  color VARCHAR(20) DEFAULT '#007acc',
  `order` INT NOT NULL DEFAULT 0,
  is_archived TINYINT(1) NOT NULL DEFAULT 0,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  INDEX idx_categories_order (`order`, is_archived)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Category moderators
CREATE TABLE category_moderators (
  category_id BIGINT UNSIGNED NOT NULL,
  user_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (category_id, user_id),
  INDEX idx_cat_mod_category (category_id),
  INDEX idx_cat_mod_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Tags
CREATE TABLE tags (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(100) NOT NULL,
  slug VARCHAR(100) NOT NULL UNIQUE,
  description VARCHAR(255),
  color VARCHAR(20) DEFAULT '#6B7280',
  usage_count INT NOT NULL DEFAULT 0,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  INDEX idx_tags_usage (usage_count DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Insert default categories
INSERT INTO categories (name, slug, description, color, `order`) VALUES
('通用讨论', 'general', '通用话题讨论区', '#007acc', 1),
('技术交流', 'tech', '技术相关讨论', '#10B981', 2),
('产品反馈', 'feedback', '产品使用反馈和建议', '#F59E0B', 3),
('公告通知', 'announcements', '官方公告和通知', '#EF4444', 0);

-- Insert default tags
INSERT INTO tags (name, slug, description, color) VALUES
('问题', 'question', '求助类问题', '#3B82F6'),
('讨论', 'discussion', '开放式讨论', '#8B5CF6'),
('分享', 'share', '经验分享', '#10B981'),
('建议', 'suggestion', '功能建议', '#F59E0B'),
('反馈', 'feedback', '问题反馈', '#EF4444');