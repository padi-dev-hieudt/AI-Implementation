/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/pages/admin.js
   Admin dashboard — category, tag, post, and user management.
   Depends on: ApiService, UserService, Utils, Auth (via Auth.ready)
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── AdminPanel ──────────────────────────────────────────────────
   Handles the /admin page with four lazy-loaded Bootstrap tabs.
   ─────────────────────────────────────────────────────────────── */
const AdminPanel = (() => {
    const _loaded   = new Set();   // tracks which tabs have been loaded
    const PAGE_SIZE = 20;

    // State for paginated tabs
    let _postPage = 1;
    let _userPage = 1;

    async function init() {
        // Security: redirect non-admin users immediately (server also enforces via [Authorize])
        if (!Auth.user || Auth.user.role !== 'Admin') {
            location.href = '/';
            return;
        }

        // Load the first tab (Categories) immediately
        await _loadCategories();
        _loaded.add('categories');

        // Lazy-load other tabs on first activation
        document.getElementById('adminTabs')?.addEventListener('shown.bs.tab', async e => {
            const target = e.target.dataset.bsTarget;
            if (target === '#pane-tags'  && !_loaded.has('tags')) {
                await _loadTags();
                _loaded.add('tags');
            }
            if (target === '#pane-posts' && !_loaded.has('posts')) {
                await _loadPosts(1);
                _loaded.add('posts');
            }
            if (target === '#pane-users' && !_loaded.has('users')) {
                await _loadUsers(1);
                _loaded.add('users');
            }
        });

        _wireCategoryModal();
        _wireTagModal();
    }

    // ════════════════════════════════════════════════════════════════
    // CATEGORIES
    // ════════════════════════════════════════════════════════════════

    async function _loadCategories() {
        const wrap = document.getElementById('catTableWrap');
        const err  = document.getElementById('catError');
        if (!wrap) return;
        try {
            err?.classList.add('d-none');
            const res  = await ApiService.get('/api/category');
            const cats = res.data || [];
            if (cats.length === 0) {
                wrap.innerHTML = '<p class="text-muted small">Chưa có danh mục nào.</p>';
                return;
            }
            wrap.innerHTML = `
                <div class="table-responsive">
                <table class="table table-sm table-hover align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th style="width:60px">ID</th>
                      <th>Tên</th>
                      <th class="d-none d-md-table-cell">Mô tả</th>
                      <th style="width:80px">Mặc định</th>
                      <th style="width:80px">Thứ tự</th>
                      <th class="d-none d-md-table-cell" style="width:70px">Bài viết</th>
                      <th style="width:120px"></th>
                    </tr>
                  </thead>
                  <tbody>
                    ${cats.map(_catRow).join('')}
                  </tbody>
                </table>
                </div>`;

            // Wire edit / delete buttons
            wrap.querySelectorAll('.btn-cat-edit').forEach(btn => {
                btn.addEventListener('click', () => _openEditCategory(btn));
            });
            wrap.querySelectorAll('.btn-cat-delete').forEach(btn => {
                btn.addEventListener('click', () => _deleteCategory(btn));
            });
        } catch (e) {
            if (err) { err.textContent = e.message; err.classList.remove('d-none'); }
        }
    }

    function _catRow(c) {
        const canDelete = !c.isDefault && c.postCount === 0;
        return `<tr data-cat-id="${c.id}">
            <td class="text-muted small">${c.id}</td>
            <td class="fw-medium">${Utils.escapeHtml(c.name)}</td>
            <td class="text-muted small d-none d-md-table-cell">${Utils.escapeHtml(c.description || '')}</td>
            <td class="text-center">
                ${c.isDefault ? '<i class="bi bi-check-circle-fill text-success"></i>' : ''}
            </td>
            <td class="text-center">${c.sortOrder}</td>
            <td class="d-none d-md-table-cell text-center">${c.postCount}</td>
            <td class="text-end">
                <button class="btn btn-outline-secondary btn-sm btn-cat-edit me-1"
                        data-cat='${JSON.stringify(c)}' title="Chỉnh sửa">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-outline-danger btn-sm btn-cat-delete"
                        data-cat-id="${c.id}"
                        ${canDelete ? '' : 'disabled title="Không thể xoá danh mục mặc định hoặc đang có bài viết"'}>
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        </tr>`;
    }

    function _wireCategoryModal() {
        const form    = document.getElementById('categoryForm');
        const modal   = document.getElementById('categoryModal');
        if (!form || !modal) return;

        document.getElementById('btnAddCategory')?.addEventListener('click', () => {
            _resetCategoryModal();
            bootstrap.Modal.getOrCreateInstance(modal).show();
        });

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const id          = parseInt(document.getElementById('categoryId').value, 10);
            const name        = document.getElementById('categoryName').value.trim();
            const description = document.getElementById('categoryDescription').value.trim();
            const sortOrder   = parseInt(document.getElementById('categorySortOrder').value, 10) || 0;
            const isDefault   = document.getElementById('categoryIsDefault').checked;
            const errEl       = document.getElementById('categoryModalError');
            const spinner     = document.getElementById('catSaveSpinner');
            const btn         = document.getElementById('btnSaveCategory');

            if (!name) {
                if (errEl) { errEl.textContent = 'Vui lòng nhập tên danh mục.'; errEl.classList.remove('d-none'); }
                return;
            }

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const dto = { name, description, sortOrder, isDefault };
                if (id === 0) {
                    await ApiService.post('/api/category', dto);
                    Utils.flash('Đã thêm danh mục.', 'success');
                } else {
                    await ApiService.put(`/api/category/${id}`, dto);
                    Utils.flash('Đã cập nhật danh mục.', 'success');
                }

                bootstrap.Modal.getOrCreateInstance(modal).hide();
                await _loadCategories();
            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    function _resetCategoryModal() {
        document.getElementById('categoryId').value              = '0';
        document.getElementById('categoryName').value            = '';
        document.getElementById('categoryDescription').value     = '';
        document.getElementById('categorySortOrder').value       = '0';
        document.getElementById('categoryIsDefault').checked     = false;
        document.getElementById('categoryModalTitle').textContent = 'Thêm danh mục';
        document.getElementById('categoryModalError')?.classList.add('d-none');
    }

    function _openEditCategory(btn) {
        const c = JSON.parse(btn.dataset.cat);
        document.getElementById('categoryId').value              = c.id;
        document.getElementById('categoryName').value            = c.name;
        document.getElementById('categoryDescription').value     = c.description || '';
        document.getElementById('categorySortOrder').value       = c.sortOrder;
        document.getElementById('categoryIsDefault').checked     = c.isDefault;
        document.getElementById('categoryModalTitle').textContent = 'Chỉnh sửa danh mục';
        document.getElementById('categoryModalError')?.classList.add('d-none');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryModal')).show();
    }

    async function _deleteCategory(btn) {
        const id = parseInt(btn.dataset.catId, 10);
        if (!confirm('Bạn có chắc muốn xoá danh mục này không?')) return;
        try {
            await ApiService.delete(`/api/category/${id}`);
            Utils.flash('Đã xoá danh mục.', 'success');
            await _loadCategories();
        } catch (e) {
            Utils.flash(e.message, 'danger');
        }
    }

    // ════════════════════════════════════════════════════════════════
    // TAGS
    // ════════════════════════════════════════════════════════════════

    async function _loadTags() {
        const wrap = document.getElementById('tagTableWrap');
        const err  = document.getElementById('tagError');
        if (!wrap) return;
        try {
            err?.classList.add('d-none');
            const res  = await ApiService.get('/api/tag');
            const tags = res.data || [];
            if (tags.length === 0) {
                wrap.innerHTML = '<p class="text-muted small">Chưa có thẻ nào.</p>';
                return;
            }
            wrap.innerHTML = `
                <div class="table-responsive">
                <table class="table table-sm table-hover align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th style="width:60px">ID</th>
                      <th>Tên thẻ</th>
                      <th style="width:80px">Bài viết</th>
                      <th style="width:120px"></th>
                    </tr>
                  </thead>
                  <tbody>
                    ${tags.map(_tagRow).join('')}
                  </tbody>
                </table>
                </div>`;

            wrap.querySelectorAll('.btn-tag-edit').forEach(btn => {
                btn.addEventListener('click', () => _openEditTag(btn));
            });
            wrap.querySelectorAll('.btn-tag-delete').forEach(btn => {
                btn.addEventListener('click', () => _deleteTag(btn));
            });
        } catch (e) {
            if (err) { err.textContent = e.message; err.classList.remove('d-none'); }
        }
    }

    function _tagRow(t) {
        return `<tr data-tag-id="${t.id}">
            <td class="text-muted small">${t.id}</td>
            <td><span class="badge bg-secondary">${Utils.escapeHtml(t.name)}</span></td>
            <td class="text-center">${t.postCount}</td>
            <td class="text-end">
                <button class="btn btn-outline-secondary btn-sm btn-tag-edit me-1"
                        data-tag='${JSON.stringify(t)}' title="Chỉnh sửa">
                    <i class="bi bi-pencil"></i>
                </button>
                <button class="btn btn-outline-danger btn-sm btn-tag-delete"
                        data-tag-id="${t.id}" title="Xoá">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        </tr>`;
    }

    function _wireTagModal() {
        const form  = document.getElementById('tagForm');
        const modal = document.getElementById('tagModal');
        if (!form || !modal) return;

        document.getElementById('btnAddTag')?.addEventListener('click', () => {
            _resetTagModal();
            bootstrap.Modal.getOrCreateInstance(modal).show();
        });

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const id    = parseInt(document.getElementById('tagId').value, 10);
            const name  = document.getElementById('tagName').value.trim();
            const errEl = document.getElementById('tagModalError');
            const spinner = document.getElementById('tagSaveSpinner');
            const btn   = document.getElementById('btnSaveTag');

            if (!name) {
                if (errEl) { errEl.textContent = 'Vui lòng nhập tên thẻ.'; errEl.classList.remove('d-none'); }
                return;
            }

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const dto = { name };
                if (id === 0) {
                    await ApiService.post('/api/tag', dto);
                    Utils.flash('Đã thêm thẻ.', 'success');
                } else {
                    await ApiService.put(`/api/tag/${id}`, dto);
                    Utils.flash('Đã cập nhật thẻ.', 'success');
                }

                bootstrap.Modal.getOrCreateInstance(modal).hide();
                await _loadTags();
            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    function _resetTagModal() {
        document.getElementById('tagId').value               = '0';
        document.getElementById('tagName').value             = '';
        document.getElementById('tagModalTitle').textContent = 'Thêm thẻ';
        document.getElementById('tagModalError')?.classList.add('d-none');
    }

    function _openEditTag(btn) {
        const t = JSON.parse(btn.dataset.tag);
        document.getElementById('tagId').value               = t.id;
        document.getElementById('tagName').value             = t.name;
        document.getElementById('tagModalTitle').textContent = 'Chỉnh sửa thẻ';
        document.getElementById('tagModalError')?.classList.add('d-none');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('tagModal')).show();
    }

    async function _deleteTag(btn) {
        const id = parseInt(btn.dataset.tagId, 10);
        if (!confirm('Bạn có chắc muốn xoá thẻ này không? Thao tác này sẽ gỡ thẻ khỏi tất cả bài viết.')) return;
        try {
            await ApiService.delete(`/api/tag/${id}`);
            Utils.flash('Đã xoá thẻ.', 'success');
            await _loadTags();
        } catch (e) {
            Utils.flash(e.message, 'danger');
        }
    }

    // ════════════════════════════════════════════════════════════════
    // POSTS
    // ════════════════════════════════════════════════════════════════

    async function _loadPosts(page) {
        _postPage = page;
        const wrap = document.getElementById('postTableWrap');
        const err  = document.getElementById('postError');
        if (!wrap) return;
        try {
            err?.classList.add('d-none');
            const res   = await ApiService.get(`/api/post?page=${page}&pageSize=${PAGE_SIZE}`);
            const paged = res.data;
            const posts = paged?.items || [];

            if (posts.length === 0) {
                wrap.innerHTML = '<p class="text-muted small">Chưa có bài viết nào.</p>';
                return;
            }

            wrap.innerHTML = `
                <div class="table-responsive">
                <table class="table table-sm table-hover align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th style="width:60px">ID</th>
                      <th>Tiêu đề</th>
                      <th class="d-none d-md-table-cell">Tác giả</th>
                      <th class="d-none d-lg-table-cell">Danh mục</th>
                      <th style="width:90px">Trạng thái</th>
                      <th style="width:130px"></th>
                    </tr>
                  </thead>
                  <tbody>
                    ${posts.map(_postRow).join('')}
                  </tbody>
                </table>
                </div>`;

            wrap.querySelectorAll('.btn-post-toggle').forEach(btn => {
                btn.addEventListener('click', () => _togglePost(btn));
            });
            wrap.querySelectorAll('.btn-post-delete').forEach(btn => {
                btn.addEventListener('click', () => _deletePost(btn));
            });

            Utils.renderPagination(
                paged,
                { wrap: 'postPaginationWrapper', info: 'postPaginationInfo', ul: 'postPagination' },
                p => _loadPosts(p),
                'bài viết'
            );
        } catch (e) {
            if (err) { err.textContent = e.message; err.classList.remove('d-none'); }
        }
    }

    function _postRow(p) {
        const statusBadge = p.isClosed
            ? '<span class="badge bg-secondary">Đóng</span>'
            : '<span class="badge bg-success">Mở</span>';
        const toggleLabel = p.isClosed ? 'Mở lại' : 'Đóng';
        const toggleIcon  = p.isClosed ? 'bi-unlock' : 'bi-lock';
        const slug        = Utils.slugify(p.title);
        return `<tr data-post-id="${p.id}">
            <td class="text-muted small">${p.id}</td>
            <td>
                <a href="/post/${slug}/${p.id}" class="fw-medium text-decoration-none" target="_blank">
                    ${Utils.escapeHtml(p.title)}
                </a>
            </td>
            <td class="text-muted small d-none d-md-table-cell">${Utils.escapeHtml(p.username)}</td>
            <td class="text-muted small d-none d-lg-table-cell">${Utils.escapeHtml(p.categoryName)}</td>
            <td>${statusBadge}</td>
            <td class="text-end">
                <button class="btn btn-outline-secondary btn-sm btn-post-toggle me-1"
                        data-post-id="${p.id}" data-is-closed="${p.isClosed}"
                        title="${toggleLabel}">
                    <i class="bi ${toggleIcon}"></i>
                </button>
                <button class="btn btn-outline-danger btn-sm btn-post-delete"
                        data-post-id="${p.id}" title="Xoá bài viết">
                    <i class="bi bi-trash"></i>
                </button>
            </td>
        </tr>`;
    }

    async function _togglePost(btn) {
        const id       = parseInt(btn.dataset.postId, 10);
        const isClosed = btn.dataset.isClosed === 'true';
        const action   = isClosed ? 'mở lại' : 'đóng';
        if (!confirm(`Bạn có chắc muốn ${action} bài viết này không?`)) return;
        try {
            await ApiService.put(`/api/post/${id}/close`);
            Utils.flash(`Đã ${action} bài viết.`, 'success');
            await _loadPosts(_postPage);
        } catch (e) {
            Utils.flash(e.message, 'danger');
        }
    }

    async function _deletePost(btn) {
        const id = parseInt(btn.dataset.postId, 10);
        if (!confirm('Bạn có chắc muốn xoá bài viết này không? Thao tác này không thể hoàn tác.')) return;
        try {
            await ApiService.delete(`/api/post/${id}`);
            Utils.flash('Đã xoá bài viết.', 'success');
            await _loadPosts(_postPage);
        } catch (e) {
            Utils.flash(e.message, 'danger');
        }
    }

    // ════════════════════════════════════════════════════════════════
    // USERS
    // ════════════════════════════════════════════════════════════════

    async function _loadUsers(page) {
        _userPage = page;
        const wrap = document.getElementById('userTableWrap');
        const err  = document.getElementById('userError');
        if (!wrap) return;
        try {
            err?.classList.add('d-none');
            const res   = await UserService.getAdminUsers(page, PAGE_SIZE);
            const paged = res.data;
            const users = paged?.items || [];

            if (users.length === 0) {
                wrap.innerHTML = '<p class="text-muted small">Chưa có người dùng nào.</p>';
                return;
            }

            wrap.innerHTML = `
                <div class="table-responsive">
                <table class="table table-sm table-hover align-middle mb-0">
                  <thead class="table-light">
                    <tr>
                      <th style="width:60px">ID</th>
                      <th>Tên người dùng</th>
                      <th class="d-none d-md-table-cell">Email</th>
                      <th style="width:80px">Vai trò</th>
                      <th style="width:90px">Trạng thái</th>
                      <th style="width:100px"></th>
                    </tr>
                  </thead>
                  <tbody>
                    ${users.map(_userRow).join('')}
                  </tbody>
                </table>
                </div>`;

            wrap.querySelectorAll('.btn-user-toggle').forEach(btn => {
                btn.addEventListener('click', () => _toggleUser(btn));
            });

            Utils.renderPagination(
                paged,
                { wrap: 'userPaginationWrapper', info: 'userPaginationInfo', ul: 'userPagination' },
                p => _loadUsers(p),
                'người dùng'
            );
        } catch (e) {
            if (err) { err.textContent = e.message; err.classList.remove('d-none'); }
        }
    }

    function _userRow(u) {
        const isSelf      = Auth.user && Auth.user.id === u.id;
        const statusBadge = u.isActive
            ? '<span class="badge bg-success">Hoạt động</span>'
            : '<span class="badge bg-danger">Bị khoá</span>';
        const toggleLabel = u.isActive ? 'Khoá' : 'Mở khoá';
        const toggleClass = u.isActive ? 'btn-outline-warning' : 'btn-outline-success';
        return `<tr data-user-id="${u.id}">
            <td class="text-muted small">${u.id}</td>
            <td>
                <a href="/profile/${u.id}" class="fw-medium text-decoration-none" target="_blank">
                    ${Utils.avatarHtml(u.username, 'me-2')}${Utils.escapeHtml(u.username)}
                </a>
            </td>
            <td class="text-muted small d-none d-md-table-cell">${Utils.escapeHtml(u.email)}</td>
            <td>
                <span class="badge badge-role-${(u.role || 'user').toLowerCase()}">${Utils.escapeHtml(u.role)}</span>
            </td>
            <td>${statusBadge}</td>
            <td class="text-end">
                <button class="btn ${toggleClass} btn-sm btn-user-toggle"
                        data-user-id="${u.id}" data-is-active="${u.isActive}"
                        ${isSelf ? 'disabled title="Không thể khoá chính mình"' : `title="${toggleLabel}"`}>
                    <i class="bi bi-${u.isActive ? 'lock' : 'unlock'}"></i>
                    <span class="d-none d-lg-inline ms-1">${toggleLabel}</span>
                </button>
            </td>
        </tr>`;
    }

    async function _toggleUser(btn) {
        const id       = parseInt(btn.dataset.userId, 10);
        const isActive = btn.dataset.isActive === 'true';
        const action   = isActive ? 'khoá' : 'mở khoá';
        if (!confirm(`Bạn có chắc muốn ${action} tài khoản này không?`)) return;
        try {
            await UserService.toggleUserActive(id);
            Utils.flash(`Đã ${action} tài khoản.`, 'success');
            await _loadUsers(_userPage);
        } catch (e) {
            Utils.flash(e.message, 'danger');
        }
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.ready;
    await AdminPanel.init();
});
