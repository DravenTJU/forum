-- =====================================================================
-- Forum System Test Data Script
-- 
-- Description: Generate comprehensive test data for Discourse-style forum
-- Version: 1.0
-- Created: 2025-08-25
-- 
-- Included Data:
-- - User accounts (different roles: admin, moderator, regular users)
-- - Categories and tags
-- - Topic posts (including different statuses: pinned, locked, normal)
-- - Replies (including quotes and mentions)
-- - User role permissions
-- =====================================================================

SET FOREIGN_KEY_CHECKS = 0;
SET AUTOCOMMIT = 0;

START TRANSACTION;

-- Clean existing test data (preserve system default data)
DELETE FROM post_mentions WHERE post_id > 0;
DELETE FROM posts WHERE id > 0;
DELETE FROM topic_tags WHERE topic_id > 0;
DELETE FROM topics WHERE id > 0;
DELETE FROM category_moderators WHERE user_id > 0;
DELETE FROM user_roles WHERE user_id > 0;
DELETE FROM refresh_tokens WHERE user_id > 0;
DELETE FROM email_verification_tokens WHERE user_id > 0;
DELETE FROM users WHERE id > 0;

-- Reset auto-increment IDs
ALTER TABLE users AUTO_INCREMENT = 1;
ALTER TABLE topics AUTO_INCREMENT = 1;
ALTER TABLE posts AUTO_INCREMENT = 1;

-- =====================================================================
-- 1. Insert test users
-- =====================================================================

-- Admin users
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(1, 'admin', 'admin@forum.example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=admin', 'System administrator, responsible for forum operations and management', NOW(), NOW() - INTERVAL 30 DAY),
(2, 'moderator', 'mod@forum.example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=moderator', 'Moderator, maintaining forum order', NOW(), NOW() - INTERVAL 25 DAY);

-- Active users
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(3, 'developer_jane', 'jane@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=jane', 'Full-stack developer, focused on modern web technologies', NOW(), NOW() - INTERVAL 20 DAY),
(4, 'backend_bob', 'bob@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=bob', 'Backend development expert, familiar with microservices architecture', NOW() - INTERVAL 30 MINUTE, NOW() - INTERVAL 18 DAY),
(5, 'frontend_alice', 'alice@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=alice', 'UI/UX designer and frontend developer', NOW() - INTERVAL 2 HOUR, NOW() - INTERVAL 15 DAY),
(6, 'devops_charlie', 'charlie@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=charlie', 'DevOps engineer, containerization and cloud-native expert', NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 12 DAY);

-- Regular users
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(7, 'newbie_david', 'david@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=david', 'Programming newbie, currently learning web development', NOW() - INTERVAL 3 HOUR, NOW() - INTERVAL 10 DAY),
(8, 'student_emma', 'emma@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=emma', 'Computer science student, interested in AI and machine learning', NOW() - INTERVAL 6 HOUR, NOW() - INTERVAL 8 DAY),
(9, 'freelancer_frank', 'frank@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=frank', 'Freelancer, full-stack independent developer', NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 6 DAY),
(10, 'senior_grace', 'grace@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 1, 'https://api.dicebear.com/7.x/avataaars/svg?seed=grace', 'Senior technical architect, 15 years of development experience', NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 4 DAY);

-- Inactive users and test statuses
INSERT INTO users (id, username, email, password_hash, status, email_verified, avatar_url, bio, last_seen_at, created_at) VALUES
(11, 'inactive_henry', 'henry@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'active', 0, NULL, 'Test account - unverified email', NULL, NOW() - INTERVAL 3 DAY),
(12, 'suspended_user', 'suspended@example.com', 'u+IPdC4Ogn8BC5Bgfz1auYMCOnjuFtqX1IsBlfHee0Q=:GmwBJNmFNtydcoLxMZg++A==', 'suspended', 1, NULL, 'Account temporarily suspended for violations', NOW() - INTERVAL 7 DAY, NOW() - INTERVAL 2 DAY);

-- =====================================================================
-- 2. Assign user roles
-- =====================================================================

INSERT INTO user_roles (user_id, role) VALUES
-- Administrator
(1, 'admin'),
(1, 'mod'),
(1, 'user'),

-- Moderator
(2, 'mod'),
(2, 'user'),

-- Regular users
(3, 'user'),
(4, 'user'),
(5, 'user'),
(6, 'user'),
(7, 'user'),
(8, 'user'),
(9, 'user'),
(10, 'user'),
(11, 'user'),
(12, 'user');

-- =====================================================================
-- 3. Assign moderator permissions (category management)
-- =====================================================================

INSERT INTO category_moderators (category_id, user_id) VALUES
-- admin manages all categories
(1, 1), (2, 1), (3, 1), (4, 1),
-- moderator manages technical discussion and product feedback
(2, 2), (3, 2);

-- =====================================================================
-- 4. Update tag usage count (prepare for subsequent topic data)
-- =====================================================================

UPDATE tags SET usage_count = 0;

-- =====================================================================
-- 5. Create topic posts
-- =====================================================================

-- Topics in technical discussion category
INSERT INTO topics (id, title, slug, author_id, category_id, is_pinned, is_locked, reply_count, view_count, last_posted_at, last_poster_id, created_at, updated_at) VALUES
(1, 'Welcome to the Forum! Beginner''s Essential Guide', 'welcome-guide', 1, 1, 1, 0, 8, 245, NOW() - INTERVAL 2 HOUR, 7, NOW() - INTERVAL 25 DAY, NOW() - INTERVAL 2 HOUR),
(2, 'React 18 New Features Deep Dive', 'react-18-features', 3, 2, 1, 0, 12, 156, NOW() - INTERVAL 3 HOUR, 5, NOW() - INTERVAL 20 DAY, NOW() - INTERVAL 3 HOUR),
(3, 'ASP.NET Core Performance Optimization Practice', 'aspnet-core-performance', 4, 2, 0, 0, 15, 189, NOW() - INTERVAL 1 HOUR, 10, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 1 HOUR),
(4, 'Docker Containerization Deployment Complete Guide', 'docker-deployment-guide', 6, 2, 0, 0, 9, 134, NOW() - INTERVAL 5 HOUR, 3, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 5 HOUR),
(5, 'Frontend State Management Library Comparison Analysis', 'frontend-state-management', 5, 2, 0, 0, 7, 98, NOW() - INTERVAL 8 HOUR, 9, NOW() - INTERVAL 12 DAY, NOW() - INTERVAL 8 HOUR),
(6, 'MySQL Index Optimization Strategies', 'mysql-index-optimization', 10, 2, 0, 0, 6, 87, NOW() - INTERVAL 12 HOUR, 4, NOW() - INTERVAL 10 DAY, NOW() - INTERVAL 12 HOUR),
(7, 'Newbie Help: How to Start Learning Programming?', 'beginner-programming-help', 7, 1, 0, 0, 11, 67, NOW() - INTERVAL 4 HOUR, 8, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 4 HOUR),
(8, 'Suggestions for New Forum Features', 'forum-feature-suggestions', 9, 3, 0, 1, 5, 45, NOW() - INTERVAL 2 DAY, 2, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 2 DAY),
(9, 'Share: My Development Toolchain Configuration', 'my-dev-toolchain', 3, 1, 0, 0, 3, 89, NOW() - INTERVAL 1 DAY, 6, NOW() - INTERVAL 4 DAY, NOW() - INTERVAL 1 DAY),
(10, '[SOLVED] TypeScript Type Inference Issue', 'typescript-type-inference', 8, 2, 0, 0, 8, 76, NOW() - INTERVAL 18 HOUR, 3, NOW() - INTERVAL 3 DAY, NOW() - INTERVAL 18 HOUR);

-- =====================================================================
-- 6. Topic-tag associations
-- =====================================================================

INSERT INTO topic_tags (topic_id, tag_id) VALUES
-- Welcome guide: discussion
(1, 2),
-- React 18: discussion, sharing
(2, 2), (2, 3),
-- ASP.NET Core: sharing
(3, 3),
-- Docker guide: sharing, discussion
(4, 3), (4, 2),
-- State management: discussion
(5, 2),
-- MySQL optimization: sharing
(6, 3),
-- Newbie help: question
(7, 1),
-- Feature suggestions: suggestion
(8, 4),
-- Toolchain: sharing
(9, 3),
-- TypeScript issue: question, sharing
(10, 1), (10, 3);

-- Update tag usage count
UPDATE tags SET usage_count = (
  SELECT COUNT(*) FROM topic_tags WHERE topic_tags.tag_id = tags.id
);

-- =====================================================================
-- 7. Create post content (original posts and replies)
-- =====================================================================

-- Topic 1: Welcome guide posts
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(1, 1, 1, '# Welcome to Our Tech Forum! 🎉

Hello, fellow developers!

Welcome to our technical community! This is a platform built specifically for developers, whether you''re a beginner just starting out or an experienced senior engineer, you can find your place here.

## Forum Features

- **Categorized Discussions**: We have different technical categories to help you find topics of interest
- **Tagging System**: Quickly filter and locate relevant content through tags  
- **Real-time Interaction**: Support for real-time replies and notifications for smoother discussions
- **Personal Profiles**: Showcase your technical expertise and contributions

## Posting Guidelines

1. **Choose the Right Category**: Make sure your post is in the correct category
2. **Use Appropriate Tags**: Help others find your content more easily
3. **Clear Titles**: Make it clear what you want to discuss at first glance
4. **Detailed Content**: Provide sufficient background information and specific problem descriptions

Looking forward to everyone''s active participation as we build a high-quality technical exchange community together!', NULL, NOW() - INTERVAL 25 DAY, NOW() - INTERVAL 25 DAY);

-- Replies
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(2, 1, 3, 'Thanks for the detailed introduction, admin! The forum features look very comprehensive, looking forward to learning more here.', 1, NOW() - INTERVAL 24 DAY, NOW() - INTERVAL 24 DAY),
(3, 1, 4, 'The interface design is really nice, very smooth to use 👍', 1, NOW() - INTERVAL 23 DAY, NOW() - INTERVAL 23 DAY),
(4, 1, 5, 'I especially love the real-time notification feature, it makes discussions much more efficient!', 1, NOW() - INTERVAL 22 DAY, NOW() - INTERVAL 22 DAY),
(5, 1, 7, 'As a newbie, this guide is very helpful to me. Are there any recommended learning paths?', 1, NOW() - INTERVAL 20 DAY, NOW() - INTERVAL 20 DAY),
(6, 1, 2, '@newbie_david We will gradually publish some learning resources, please follow the technical discussion category.', 5, NOW() - INTERVAL 19 DAY, NOW() - INTERVAL 19 DAY),
(7, 1, 8, 'The forum''s Markdown support is great, makes it easy to share code snippets!', 1, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 18 DAY),
(8, 1, 6, 'Hope to see more discussions about DevOps and cloud-native technologies.', 1, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 15 DAY),
(9, 1, 7, 'Thanks @moderator for the reply! I will pay more attention to learning resources.', 6, NOW() - INTERVAL 2 HOUR, NOW() - INTERVAL 2 HOUR);

-- Topic 2: React 18 posts
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(10, 2, 3, '# React 18 New Features Deep Dive

React 18 brings many exciting new features, and today I want to share my experience using them.

## Major New Features

### 1. Concurrent Features
React 18 introduces concurrent features, which is the biggest change:

```jsx
// New createRoot API
import { createRoot } from ''react-dom/client'';
const root = createRoot(container);
root.render(<App />);
```

### 2. Automatic Batching
Now all state updates are automatically batched:

```jsx
// In React 18, these updates will be automatically batched
setTimeout(() => {
  setCount(c => c + 1);
  setFlag(f => !f);
}, 1000);
```

### 3. Suspense Improvements
Suspense now supports more scenarios, including data fetching:

```jsx
<Suspense fallback={<Loading />}>
  <UserProfile userId={userId} />
</Suspense>
```

## Upgrade Recommendations

1. **Gradual Migration**: No need to rewrite all code at once
2. **Thorough Testing**: New concurrent features may affect component behavior
3. **Focus on Performance**: Leverage new features to optimize user experience

What issues have you encountered during the upgrade process?', NULL, NOW() - INTERVAL 20 DAY, NOW() - INTERVAL 20 DAY);

INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(11, 2, 5, 'Very detailed summary! I''ve started using React 18 in my project, and Automatic Batching really improves performance.

However, there''s one thing to note: if your code relies on synchronous state updates, you might need to make adjustments:

```jsx
// If you need to force synchronous updates, you can use flushSync
import { flushSync } from ''react-dom'';

flushSync(() => {
  setCount(count + 1);
});
```', 10, NOW() - INTERVAL 19 DAY, NOW() - INTERVAL 19 DAY),
(12, 2, 4, 'Our team is still evaluating the upgrade risks. @developer_jane What compatibility issues did you encounter during the upgrade?', 10, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 18 DAY),
(13, 2, 3, '@backend_bob Overall compatibility is very good, mainly some third-party libraries might need updates. I suggest trying it in a test environment first.

Also, StrictMode is stricter in development environment, which might reveal some previously hidden issues.', 12, NOW() - INTERVAL 17 DAY, NOW() - INTERVAL 17 DAY),
(14, 2, 7, 'As a newbie, I''d like to ask: what impact does React 18 have on the learning curve? Should I start learning directly from version 18?', 10, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 15 DAY),
(15, 2, 10, '@newbie_david I suggest learning React 18 directly, the new features are designed more intuitively and it''s the future trend.', 14, NOW() - INTERVAL 14 DAY, NOW() - INTERVAL 14 DAY),
(16, 2, 9, 'Concurrent Features are indeed very powerful, especially helpful for performance optimization of complex applications.', 10, NOW() - INTERVAL 10 DAY, NOW() - INTERVAL 10 DAY),
(17, 2, 6, 'We''ve been using it in production for several months, stability is great. Recommend everyone to upgrade!', 10, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 8 DAY),
(18, 2, 8, 'Looking forward to more practical case studies on React 18 performance optimization!', 10, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 6 DAY),
(19, 2, 5, '@student_emma I''m planning to write a detailed article about React 18 performance optimization, stay tuned!', 18, NOW() - INTERVAL 3 HOUR, NOW() - INTERVAL 3 HOUR);

-- Topic 3: ASP.NET Core performance optimization
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(20, 3, 4, '# ASP.NET Core Performance Optimization Practice Sharing

Recently conducted a round of performance optimization in our project, and I want to share some practical experiences with everyone.

## Database Optimization

### 1. Using Dapper Instead of EF Core
In high-performance scenarios, Dapper indeed performs better:

```csharp
// Dapper query example
var users = await connection.QueryAsync<User>(
    "SELECT * FROM users WHERE status = @status",
    new { status = "active" });
```

### 2. Connection Pool Optimization
```csharp
// Configure connection pool
services.AddScoped<IDbConnectionFactory>(_ => 
    new MySqlConnectionFactory(connectionString, new MySqlConnectionPoolSettings
    {
        MaxPoolSize = 100,
        MinPoolSize = 10,
        ConnectionTimeout = 30
    }));
```

## Caching Strategy

### 1. Memory Cache
```csharp
services.AddMemoryCache(options =>
{
    options.SizeLimit = 1000;
    options.CompactionPercentage = 0.25;
});
```

### 2. Distributed Cache (Redis)
```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "ForumApp";
});
```

## API Performance Optimization

### 1. Response Caching
```csharp
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "categoryId" })]
public async Task<IActionResult> GetTopics(int categoryId)
{
    // Implementation logic
}
```

### 2. Async Programming Best Practices
```csharp
// Avoid blocking calls
public async Task<List<Topic>> GetTopicsAsync()
{
    return await _repository.GetTopicsAsync().ConfigureAwait(false);
}
```

Through these optimizations, our API response time dropped from an average of 300ms to below 50ms!', NULL, NOW() - INTERVAL 18 DAY, NOW() - INTERVAL 18 DAY);

-- Continue adding more replies...
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(21, 3, 10, 'Very practical sharing! We are also considering migrating from EF Core to Dapper, the performance improvement is indeed obvious.

Here''s a small tip: when using Dapper, consider using `QueryFirstOrDefaultAsync` to avoid unnecessary memory allocation:

```csharp
var user = await connection.QueryFirstOrDefaultAsync<User>(
    "SELECT * FROM users WHERE id = @id", 
    new { id });
```', 20, NOW() - INTERVAL 17 DAY, NOW() - INTERVAL 17 DAY),
(22, 3, 6, 'Regarding caching strategies, I suggest also considering cache penetration and cache avalanche issues. You can use bloom filters or set random expiration times.', 20, NOW() - INTERVAL 16 DAY, NOW() - INTERVAL 16 DAY),
(23, 3, 3, '@backend_bob Have you done any stress testing? I''d like to know the specific QPS improvement data.', 20, NOW() - INTERVAL 15 DAY, NOW() - INTERVAL 15 DAY),
(24, 3, 4, '@developer_jane Our stress test results: QPS was about 500 before optimization, now it can reach 2000+, and latency dropped from P95 300ms to P95 80ms.', 23, NOW() - INTERVAL 14 DAY, NOW() - INTERVAL 14 DAY),
(25, 3, 8, 'Learned so much! May I ask what adjustments these optimization strategies need under microservices architecture?', 20, NOW() - INTERVAL 12 DAY, NOW() - INTERVAL 12 DAY),
(26, 3, 4, '@student_emma Under microservices, focus mainly on inter-service communication optimization, such as using HTTP/2, gRPC, and circuit breaker patterns.', 25, NOW() - INTERVAL 11 DAY, NOW() - INTERVAL 11 DAY),
(27, 3, 5, 'I''d like to add something about frontend optimization: working with backend Response Caching, the frontend should also implement good caching strategies, such as using ETags.', 20, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 8 DAY),
(28, 3, 9, 'This sharing is so valuable! Our project is facing performance bottlenecks, and we''re planning to optimize according to this approach.', 20, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 6 DAY),
(29, 3, 10, 'Do you have plans to write a complete performance optimization series? This kind of practical experience sharing is very helpful for everyone.', 20, NOW() - INTERVAL 1 HOUR, NOW() - INTERVAL 1 HOUR);

-- Topic 7: Newbie help posts
INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(30, 7, 7, '# Newbie Help: How to Start Learning Programming?

Hello everyone! I''m a complete beginner with zero programming experience, and I recently want to start learning web development, but I feel a bit overwhelmed facing so many tech stacks.

## My Situation
- Completely no programming experience
- Quite interested in website and application development  
- Average math foundation, but decent learning ability
- Can dedicate 2-3 hours per day for learning

## My Questions
1. **Which language should I start with?** JavaScript, Python, or others?
2. **How should I plan my learning path?** Should I learn frontend first or backend first?
3. **Any good learning resources to recommend?** 
4. **How to judge my learning progress?**
5. **When can I start doing projects?**

Hope the seniors can give some advice, thank you all! 🙏', NULL, NOW() - INTERVAL 8 DAY, NOW() - INTERVAL 8 DAY);

INSERT INTO posts (id, topic_id, author_id, content_md, reply_to_post_id, created_at, updated_at) VALUES
(31, 7, 3, 'Welcome to the programming world! @newbie_david 

As someone who''s been through this, I suggest:

## Learning Path Recommendations
1. **Start with Frontend**: HTML → CSS → JavaScript, because you can see intuitive results and feel accomplished
2. **Step by Step**: Don''t rush, build a solid foundation
3. **Learn by Doing**: Theory + practice, try hands-on for every concept you learn

## Recommended Resources
- **MDN Web Docs**: Authoritative web technology documentation
- **freeCodeCamp**: Free interactive courses
- **YouTube**: Many quality programming tutorials

I suggest spending 1 month learning HTML/CSS first, then 2 months on JavaScript basics. Feel free to ask questions anytime!', 30, NOW() - INTERVAL 7 DAY, NOW() - INTERVAL 7 DAY),
(32, 7, 10, '@newbie_david I also started from zero! Sharing some experiences:

## Learning Insights
1. **Don''t pursue perfection**: Get it working first, then optimize
2. **Taking notes is important**: Record your learning process and problems
3. **Join communities**: Communicate more with other developers

## Project Suggestions
- Month 1: Static web pages (personal resume)
- Months 2-3: Interactive web pages (calculator, todo list)
- Months 4-6: Complete small projects

Programming is a marathon, patience and passion are most important! 💪', 30, NOW() - INTERVAL 6 DAY, NOW() - INTERVAL 6 DAY),
(33, 7, 2, 'As a moderator, I''ve compiled a beginner learning roadmap for everyone to reference:

## Phase 1: Web Fundamentals (1-2 months)
- HTML5 semantic tags
- CSS3 layout and animations
- JavaScript ES6+ basics

## Phase 2: Frontend Frameworks (2-3 months)  
- Choose one framework to study deeply (recommend React or Vue)
- State management and routing

## Phase 3: Backend Introduction (3-4 months)
- Node.js + Express or other backend languages
- Database fundamentals (MySQL/MongoDB)

## Phase 4: Project Practice (ongoing)
- Complete full-stack projects
- Deployment and DevOps basics

The most important thing in learning programming is **consistency**, improve a little bit every day!', 30, NOW() - INTERVAL 5 DAY, NOW() - INTERVAL 5 DAY),
(34, 7, 5, '@newbie_david From a designer-to-frontend perspective, here are some suggestions:

## Learning Methods
1. **Visual Learning**: Look at excellent website designs and analyze implementation methods
2. **Tool Usage**: Get familiar with browser developer tools
3. **Design + Code**: Learn to convert from design mockups to code

I recommend creating several beautiful static pages first to develop aesthetic sense and coding intuition!', 30, NOW() - INTERVAL 4 DAY, NOW() - INTERVAL 4 DAY),
(35, 7, 8, 'I''m also currently learning, we can encourage each other! I''ve been studying JavaScript recently, it''s indeed challenging but very interesting.

@newbie_david How about we create a study group?', 30, NOW() - INTERVAL 3 DAY, NOW() - INTERVAL 3 DAY),
(36, 7, 7, 'Thank you all for the enthusiastic replies! @developer_jane @senior_grace @moderator @frontend_alice @student_emma 

I''ve started learning HTML according to everyone''s suggestions, and it''s indeed very rewarding! @student_emma The study group idea is great, we can discuss it privately.

Thanks again for all the seniors'' guidance! 🙏', 31, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 DAY),
(37, 7, 6, 'Seeing so many enthusiastic replies is really touching! This is the charm of the tech community.

@newbie_david If you want to learn DevOps-related knowledge in the future, feel free to come and chat with me!', 30, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 DAY),
(38, 7, 9, 'This post is very inspiring to me too! Although I''m already working, seeing the enthusiasm of newcomers reminds me of when I first started learning programming.

Learning never ends, let''s all keep going together! 🚀', 30, NOW() - INTERVAL 18 HOUR, NOW() - INTERVAL 18 HOUR),
(39, 7, 1, 'Seeing everyone''s interaction is so heartwarming! This is exactly the community atmosphere we hope to create.

I will compile a beginner resource collection and post it in the announcements section, stay tuned!', 30, NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 12 HOUR),
(40, 7, 8, 'Excellent! Looking forward to the admin''s resource collection. Having everyone''s company on the learning journey is truly wonderful~', 39, NOW() - INTERVAL 4 HOUR, NOW() - INTERVAL 4 HOUR);

-- =====================================================================
-- 8. Handle mention relationships
-- =====================================================================

INSERT INTO post_mentions (post_id, mentioned_user_id) VALUES
-- @ mentions in posts
(6, 7),   -- moderator mentions newbie_david
(9, 2),   -- newbie_david mentions moderator  
(12, 3),  -- backend_bob mentions developer_jane
(13, 4),  -- developer_jane mentions backend_bob
(14, 7),  -- newbie_david is mentioned
(15, 7),  -- senior_grace mentions newbie_david
(19, 8),  -- frontend_alice mentions student_emma
(23, 4),  -- developer_jane mentions backend_bob
(24, 3),  -- backend_bob mentions developer_jane
(25, 8),  -- student_emma is mentioned
(26, 8),  -- backend_bob mentions student_emma
(31, 7),  -- developer_jane mentions newbie_david
(32, 7),  -- senior_grace mentions newbie_david
(34, 7),  -- frontend_alice mentions newbie_david
(35, 7),  -- student_emma mentions newbie_david
(36, 3),  -- newbie_david mentions multiple people
(36, 10), 
(36, 2), 
(36, 5), 
(36, 8),
(37, 7),  -- devops_charlie mentions newbie_david
(39, 30); -- admin in the final reply

-- =====================================================================
-- 9. Update topic statistics
-- =====================================================================

-- Update topic reply count and last post time
UPDATE topics t SET 
  reply_count = (SELECT COUNT(*) - 1 FROM posts p WHERE p.topic_id = t.id AND p.is_deleted = 0),
  last_posted_at = (SELECT MAX(p.created_at) FROM posts p WHERE p.topic_id = t.id AND p.is_deleted = 0),
  last_poster_id = (SELECT p.author_id FROM posts p WHERE p.topic_id = t.id AND p.is_deleted = 0 ORDER BY p.created_at DESC LIMIT 1);

-- =====================================================================
-- 10. Add more test topics (simplified version)
-- =====================================================================

INSERT INTO topics (title, slug, author_id, category_id, is_pinned, is_locked, reply_count, view_count, last_posted_at, last_poster_id, created_at, updated_at) VALUES
('Microservices Architecture Best Practices', 'microservices-best-practices', 6, 2, 0, 0, 0, 23, NULL, NULL, NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 DAY),
('Vue 3 Composition API Usage Experience', 'vue3-composition-api', 5, 2, 0, 0, 0, 34, NULL, NULL, NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 DAY),
('Database Design Principles Discussion', 'database-design-principles', 10, 2, 0, 0, 0, 45, NULL, NULL, NOW() - INTERVAL 18 HOUR, NOW() - INTERVAL 18 HOUR),
('GitHub Actions CI/CD Configuration Sharing', 'github-actions-cicd', 6, 2, 0, 0, 0, 67, NULL, NULL, NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 12 HOUR),
('Mobile Responsive Design Tips', 'mobile-responsive-design', 5, 2, 0, 0, 0, 89, NULL, NULL, NOW() - INTERVAL 6 HOUR, NOW() - INTERVAL 6 HOUR);

-- Create initial posts for new topics
INSERT INTO posts (topic_id, author_id, content_md, created_at, updated_at) VALUES
(11, 6, '# Microservices Architecture Best Practices

When implementing microservices architecture, there are some key principles and practices...', NOW() - INTERVAL 2 DAY, NOW() - INTERVAL 2 DAY),
(12, 5, '# Vue 3 Composition API Usage Experience

Experience sharing from migrating from Options API to Composition API...', NOW() - INTERVAL 1 DAY, NOW() - INTERVAL 1 DAY),
(13, 10, '# Database Design Principles Discussion

Good database design is the foundation of system success, let''s discuss...', NOW() - INTERVAL 18 HOUR, NOW() - INTERVAL 18 HOUR),
(14, 6, '# GitHub Actions CI/CD Configuration Sharing

Sharing a complete CI/CD configuration for frontend and backend projects...', NOW() - INTERVAL 12 HOUR, NOW() - INTERVAL 12 HOUR),
(15, 5, '# Mobile Responsive Design Tips

Mobile-first responsive design practical experience...', NOW() - INTERVAL 6 HOUR, NOW() - INTERVAL 6 HOUR);

-- Add tags for some new topics
INSERT INTO topic_tags (topic_id, tag_id) VALUES
(11, 2), (11, 3),  -- Microservices: discussion, sharing
(12, 3),           -- Vue3: sharing  
(13, 2),           -- Database: discussion
(14, 3),           -- CI/CD: sharing
(15, 3);           -- Responsive: sharing

-- Update tag usage count
UPDATE tags SET usage_count = (
  SELECT COUNT(*) FROM topic_tags WHERE topic_tags.tag_id = tags.id
);

-- =====================================================================
-- 11. Create some email verification tokens (for testing)
-- =====================================================================

INSERT INTO email_verification_tokens (user_id, token, expires_at, created_at) VALUES
(11, 'verify_token_henry_' || UNIX_TIMESTAMP(), NOW() + INTERVAL 24 HOUR, NOW());

-- =====================================================================
-- 12. Commit transaction
-- =====================================================================

COMMIT;
SET AUTOCOMMIT = 1;
SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================================
-- Verify data insertion results
-- =====================================================================

SELECT 'Users created:' as Info, COUNT(*) as Count FROM users
UNION ALL
SELECT 'Topics created:' as Info, COUNT(*) as Count FROM topics
UNION ALL  
SELECT 'Posts created:' as Info, COUNT(*) as Count FROM posts
UNION ALL
SELECT 'User roles assigned:' as Info, COUNT(*) as Count FROM user_roles
UNION ALL
SELECT 'Topic-tag relations:' as Info, COUNT(*) as Count FROM topic_tags
UNION ALL
SELECT 'Post mentions:' as Info, COUNT(*) as Count FROM post_mentions;

-- =====================================================================
-- Script execution completed
-- =====================================================================

SELECT '🎉 Test data insertion completed!' as Status;
SELECT '📊 Data statistics: 12 users, 15 topics, 45 posts' as Summary;
SELECT '🔗 Includes complete relationships: user roles, topic tags, post references, user mentions' as Features;