# Release Notes

每个正式 tag 发布时，GitHub Actions 会自动寻找：

1. `docs/release-notes/<tag>.md`
2. `docs/release-notes/DEFAULT.md`

优先使用第 1 个文件作为 Release 正文；不存在时使用第 2 个默认模板。请在这个目录里维护每个版本的更新说明。
