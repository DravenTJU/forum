-- Topics
CREATE TABLE topics (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  title VARCHAR(200) NOT NULL,
  slug VARCHAR(220) NOT NULL,
  author_id BIGINT UNSIGNED NOT NULL,
  category_id BIGINT UNSIGNED NOT NULL,
  is_pinned TINYINT(1) NOT NULL DEFAULT 0,
  is_locked TINYINT(1) NOT NULL DEFAULT 0,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  reply_count INT NOT NULL DEFAULT 0,
  view_count INT NOT NULL DEFAULT 0,
  last_posted_at DATETIME(3) NULL,
  last_poster_id BIGINT UNSIGNED NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  UNIQUE KEY uq_topic_slug (category_id, slug),
  INDEX idx_topic_cat_last (category_id, is_deleted, last_posted_at DESC, id DESC),
  INDEX idx_topic_author (author_id),
  INDEX idx_topic_category (category_id),
  INDEX idx_topic_last_poster (last_poster_id),
  INDEX idx_topic_pinned_last (is_pinned DESC, last_posted_at DESC, id DESC),
  FULLTEXT KEY ftx_topic_title (title)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Topic tags relationship
CREATE TABLE topic_tags (
  topic_id BIGINT UNSIGNED NOT NULL,
  tag_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (topic_id, tag_id),
  INDEX idx_tt_topic (topic_id),
  INDEX idx_tt_tag (tag_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Posts
CREATE TABLE posts (
  id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
  topic_id BIGINT UNSIGNED NOT NULL,
  author_id BIGINT UNSIGNED NOT NULL,
  content_md MEDIUMTEXT NOT NULL,
  reply_to_post_id BIGINT UNSIGNED NULL,
  is_edited TINYINT(1) NOT NULL DEFAULT 0,
  is_deleted TINYINT(1) NOT NULL DEFAULT 0,
  deleted_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  INDEX idx_posts_topic_created (topic_id, is_deleted, created_at, id),
  INDEX idx_posts_author (author_id),
  INDEX idx_posts_topic (topic_id),
  INDEX idx_posts_reply_to (reply_to_post_id),
  FULLTEXT KEY ftx_posts_content (content_md)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Post mentions
CREATE TABLE post_mentions (
  post_id BIGINT UNSIGNED NOT NULL,
  mentioned_user_id BIGINT UNSIGNED NOT NULL,
  PRIMARY KEY (post_id, mentioned_user_id),
  INDEX idx_pm_post (post_id),
  INDEX idx_pm_user (mentioned_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;