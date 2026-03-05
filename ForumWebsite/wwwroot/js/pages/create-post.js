/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/pages/create-post.js
   Create post page.
   Depends on: ApiService, PostService, Utils, Auth (via Auth.ready)
   Quill must be loaded before this script (via @section Scripts).
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── CreatePost ──────────────────────────────────────────────────
   Handles the /post/create page form including category selection
   and optional tag picker (max 5 tags).
   ─────────────────────────────────────────────────────────────── */
const CreatePost = (() => {
    const MAX_TAGS = 5;
    let _selectedTagIds = new Set();

    async function init() {
        const loginPrompt = document.getElementById('createPostLoginPrompt');
        const formWrapper = document.getElementById('createPostFormWrapper');
        if (!loginPrompt || !formWrapper) return;

        if (!Auth.user) {
            loginPrompt.classList.remove('d-none');
            formWrapper.classList.add('d-none');
            return;
        }

        loginPrompt.classList.add('d-none');
        formWrapper.classList.remove('d-none');

        // Load category list and tag list concurrently
        await Promise.all([_loadCategories(), _loadTags()]);

        const titleInput   = document.getElementById('postTitle');
        const contentInput = document.getElementById('postContent');   // hidden input
        const titleCount   = document.getElementById('titleCharCount');
        const contentCount = document.getElementById('contentCharCount');
        const form         = document.getElementById('createPostForm');
        const errEl        = document.getElementById('createPostError');
        const spinner      = document.getElementById('createSpinner');
        const btn          = document.getElementById('btnCreatePost');

        const quill = new Quill('#postContentEditor', {
            theme: 'snow',
            placeholder: 'Nội dung bài viết (tối thiểu 10 ký tự)...',
            modules: { toolbar: _quillToolbar() }
        });

        quill.on('text-change', () => {
            const html = quill.root.innerHTML;
            if (contentInput) contentInput.value = html;
            if (contentCount) contentCount.textContent = `${quill.getText().trim().length} ký tự`;
        });

        titleInput?.addEventListener('input', () => {
            if (titleCount) titleCount.textContent = `${titleInput.value.length} / 300`;
        });

        form?.addEventListener('submit', async e => {
            e.preventDefault();
            const title       = titleInput?.value.trim();
            const content     = contentInput?.value.trim();
            const visibleText = quill.getText().trim();
            const categoryId  = parseInt(document.getElementById('postCategory')?.value || '0', 10);

            if (!title || visibleText.length < 10) {
                if (errEl) {
                    errEl.textContent = !title
                        ? 'Vui lòng nhập tiêu đề.'
                        : 'Nội dung phải có ít nhất 10 ký tự.';
                    errEl.classList.remove('d-none');
                }
                return;
            }

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                // categoryId=0 → backend resolves to default "Uncategorized"
                // tagIds: array of selected tag IDs (may be empty)
                const dto = {
                    title,
                    content,
                    categoryId,
                    tagIds: [..._selectedTagIds]
                };

                const res = await PostService.create(dto);
                Utils.flash('Đã đăng bài viết!', 'success');
                setTimeout(() => {
                    location.href = `/post/${Utils.slugify(res.data.title)}/${res.data.id}`;
                }, 500);

            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    // ── Private helpers ────────────────────────────────────────────

    async function _loadCategories() {
        const select = document.getElementById('postCategory');
        if (!select) return;

        try {
            const res        = await ApiService.get('/api/category');
            const categories = res.data || [];

            if (categories.length === 0) {
                select.innerHTML = '<option value="0">Không có danh mục</option>';
                return;
            }

            // Sort by SortOrder then Name; put IsDefault first
            categories.sort((a, b) => (a.sortOrder - b.sortOrder) || a.name.localeCompare(b.name));

            select.innerHTML = categories.map(c =>
                `<option value="${c.id}"${c.isDefault ? ' selected' : ''}>${Utils.escapeHtml(c.name)}</option>`
            ).join('');

        } catch {
            select.innerHTML = '<option value="0">Uncategorized (mặc định)</option>';
        }
    }

    async function _loadTags() {
        const container = document.getElementById('tagPickerContainer');
        if (!container) return;

        try {
            const res  = await ApiService.get('/api/tag');
            const tags = res.data || [];

            if (tags.length === 0) {
                container.innerHTML = '<span class="text-muted small">Chưa có thẻ nào.</span>';
                return;
            }

            // Render as toggle badges
            container.innerHTML = tags.map(t =>
                `<button type="button"
                         class="btn btn-sm btn-outline-secondary tag-toggle-btn"
                         data-tag-id="${t.id}"
                         data-tag-name="${Utils.escapeHtml(t.name)}">
                    ${Utils.escapeHtml(t.name)}
                 </button>`
            ).join('');

            container.querySelectorAll('.tag-toggle-btn').forEach(btn => {
                btn.addEventListener('click', () => _toggleTag(btn));
            });

        } catch {
            container.innerHTML = '<span class="text-muted small">Không tải được thẻ.</span>';
        }
    }

    function _toggleTag(btn) {
        const id = parseInt(btn.dataset.tagId, 10);

        if (_selectedTagIds.has(id)) {
            // Deselect
            _selectedTagIds.delete(id);
            btn.classList.remove('btn-secondary');
            btn.classList.add('btn-outline-secondary');
        } else {
            if (_selectedTagIds.size >= MAX_TAGS) {
                Utils.flash(`Chỉ được chọn tối đa ${MAX_TAGS} thẻ.`, 'warning');
                return;
            }
            // Select
            _selectedTagIds.add(id);
            btn.classList.remove('btn-outline-secondary');
            btn.classList.add('btn-secondary');
        }
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.ready;
    await CreatePost.init();
});
