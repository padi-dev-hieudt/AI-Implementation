/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/pages/post-detail.js
   Post detail page — loads post + comments, wires all CRUD actions.
   Depends on: ApiService, PostService, CommentService, Utils, Auth
   Quill must be loaded before this script (via @section Scripts).
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── PostDetail ──────────────────────────────────────────────────
   Loads a post and its comments; wires edit, delete, close, comment.
   ─────────────────────────────────────────────────────────────── */
const PostDetail = (() => {
    let _postId   = 0;
    let _editQuill = null;   // Quill instance for the edit-post modal

    async function init(postId) {
        _postId = postId;
        await _load();
    }

    async function _load() {
        const container = document.getElementById('postContainer');
        if (!container) return;

        try {
            const res  = await PostService.getById(_postId);
            const post = res.data;

            /* Breadcrumb title */
            const bc = document.getElementById('breadcrumbTitle');
            if (bc) bc.textContent =
                post.title.length > 60 ? post.title.substring(0, 60) + '…' : post.title;

            document.title = `${post.title} — Forum`;

            /* Canonicalize slug — silently correct the URL if it doesn't match */
            const correctSlug = Utils.slugify(post.title);
            const canonical   = `/post/${correctSlug}/${_postId}`;
            if (window.location.pathname !== canonical) {
                history.replaceState(null, '', canonical);
            }

            _renderPost(post);
            _showCommentSection(post.comments || []);

        } catch (err) {
            container.innerHTML = `
                <div class="forum-card p-5 text-center">
                    <i class="bi bi-exclamation-triangle fs-1 text-danger d-block mb-3"></i>
                    <h5>Không tìm thấy bài viết</h5>
                    <p class="text-muted">${Utils.escapeHtml(err.message)}</p>
                    <a href="/" class="btn btn-primary">Quay về trang chủ</a>
                </div>`;
        }
    }

    function _renderPost(post) {
        const container = document.getElementById('postContainer');
        if (!container) return;

        const avatar      = Utils.avatarHtml(post.username);
        const isOwner     = Auth.user && Auth.user.id === post.userId;
        const isAdmin     = Auth.user && Auth.user.role === 'Admin';
        const canEdit     = isOwner;
        const canDelete   = isOwner || isAdmin;
        const canClose    = isAdmin;
        const edited      = post.updatedAt
            ? `<span class="fst-italic text-muted small ms-1">(đã chỉnh sửa)</span>` : '';
        const closedBadge = post.isClosed
            ? `<span class="badge bg-secondary ms-2"><i class="bi bi-lock me-1"></i>Đã đóng</span>` : '';

        container.innerHTML = `
        <div class="post-detail-card">
            <div class="post-header">
                <h1 class="post-title">${Utils.escapeHtml(post.title)}${closedBadge}</h1>
                <div class="post-author-row">
                    ${avatar}
                    <div>
                        <div class="post-author-name">${Utils.escapeHtml(post.username)}</div>
                        <div class="post-author-meta">${Utils.timeAgo(post.createdAt)} ${edited}</div>
                    </div>
                </div>
            </div>
            <div class="post-body" id="postBodyContent"></div>
            <div class="post-footer">
                <div class="post-stats">
                    <span><i class="bi bi-eye me-1"></i>${Utils.fmtNum(post.viewCount)} lượt xem</span>
                    <span><i class="bi bi-chat me-1"></i>${post.commentCount || 0} bình luận</span>
                </div>
                ${(canEdit || canDelete || canClose) ? `
                <div class="post-actions">
                    ${canEdit ? `
                    <button class="btn btn-outline-secondary btn-sm" id="btnEditPost">
                        <i class="bi bi-pencil me-1"></i>Sửa
                    </button>` : ''}
                    ${canDelete ? `
                    <button class="btn btn-outline-danger btn-sm" id="btnDeletePost">
                        <i class="bi bi-trash me-1"></i>Xóa
                    </button>` : ''}
                    ${canClose ? `
                    <button class="btn btn-outline-warning btn-sm" id="btnClosePost">
                        <i class="bi bi-${post.isClosed ? 'unlock' : 'lock'} me-1"></i>${post.isClosed ? 'Mở lại' : 'Đóng'}
                    </button>` : ''}
                </div>` : ''}
            </div>
        </div>`;

        /* Safe: content was sanitized by Ganss.Xss on the server before being stored */
        const bodyEl = document.getElementById('postBodyContent');
        if (bodyEl) bodyEl.innerHTML = post.content;

        if (canEdit)   document.getElementById('btnEditPost')?.addEventListener('click', () => _showEditPost(post));
        if (canDelete) document.getElementById('btnDeletePost')?.addEventListener('click', () => _deletePost());
        if (canClose)  document.getElementById('btnClosePost')?.addEventListener('click', () => _toggleClose(post));
    }

    function _showCommentSection(comments) {
        const section  = document.getElementById('commentsSection');
        const countEl  = document.getElementById('commentCount');
        section?.classList.remove('d-none');
        if (countEl) countEl.textContent = comments.length;

        const container = document.getElementById('commentsContainer');
        if (container) {
            if (comments.length === 0) {
                container.innerHTML = `
                    <div class="text-center py-4 text-muted small">
                        <i class="bi bi-chat-square d-block fs-4 mb-2"></i>
                        Chưa có bình luận nào. Hãy là người đầu tiên!
                    </div>`;
            } else {
                container.innerHTML = comments.map(_renderComment).join('');
                _wireCommentActions(container);
            }
        }

        const formSection   = document.getElementById('commentFormSection');
        const guestPrompt   = document.getElementById('commentGuestPrompt');
        const commentAvatar = document.getElementById('commentAvatar');

        if (Auth.user) {
            formSection?.classList.remove('d-none');
            guestPrompt?.classList.add('d-none');
            if (commentAvatar) {
                commentAvatar.textContent      = Auth.user.username[0].toUpperCase();
                commentAvatar.style.background = Utils.strToColor(Auth.user.username);
            }
            _setupCommentForm();
        } else {
            formSection?.classList.add('d-none');
            guestPrompt?.classList.remove('d-none');
        }
    }

    function _renderComment(comment) {
        const avatar  = Utils.avatarHtml(comment.username, 'comment-avatar');
        const canEdit = Auth.user && (Auth.user.id === comment.userId || Auth.user.role === 'Admin');
        const edited  = comment.updatedAt
            ? `<span class="comment-edited">(đã sửa)</span>` : '';

        return `
        <div class="comment-card" data-comment-id="${comment.id}">
            ${avatar}
            <div class="comment-body">
                <div class="comment-header">
                    <span class="comment-author">${Utils.escapeHtml(comment.username)}</span>
                    <span class="comment-time">${Utils.timeAgo(comment.createdAt)}</span>
                    ${edited}
                </div>
                <div class="comment-text">${Utils.escapeHtml(comment.content)}</div>
                ${canEdit ? `
                <div class="comment-actions">
                    <button class="comment-action-btn comment-btn-edit">
                        <i class="bi bi-pencil me-1"></i>Sửa
                    </button>
                    <button class="comment-action-btn danger comment-btn-delete">
                        <i class="bi bi-trash me-1"></i>Xóa
                    </button>
                </div>` : ''}
                <div class="comment-edit-form d-none"></div>
            </div>
        </div>`;
    }

    function _wireCommentActions(container) {
        container.querySelectorAll('.comment-card[data-comment-id]').forEach(card => {
            const cid = parseInt(card.dataset.commentId, 10);

            card.querySelector('.comment-btn-edit')?.addEventListener('click', () => {
                const currentText = card.querySelector('.comment-text')?.textContent || '';
                _showEditComment(cid, currentText, card);
            });

            card.querySelector('.comment-btn-delete')?.addEventListener('click', () => {
                _deleteComment(cid, card);
            });
        });
    }

    function _setupCommentForm() {
        const input   = document.getElementById('commentInput');
        const btn     = document.getElementById('btnSubmitComment');
        const charEl  = document.getElementById('commentCharCount');
        const spinner = document.getElementById('commentSpinner');
        if (!input || !btn) return;

        input.addEventListener('input', () => {
            if (charEl) charEl.textContent = `${input.value.length} / 5000`;
        });

        btn.addEventListener('click', async () => {
            const content = input.value.trim();
            if (!content) { input.focus(); return; }
            if (content.length > 5000) {
                Utils.flash('Bình luận không được quá 5000 ký tự.', 'warning');
                return;
            }
            try {
                spinner?.classList.remove('d-none');
                btn.disabled = true;

                await CommentService.create({ postId: _postId, content });
                input.value = '';
                if (charEl) charEl.textContent = '0 / 5000';

                await _load();
                document.getElementById('commentsSection')
                    ?.scrollIntoView({ behavior: 'smooth', block: 'start' });

            } catch (err) {
                Utils.flash('Gửi bình luận thất bại: ' + err.message, 'danger');
            } finally {
                spinner?.classList.add('d-none');
                btn.disabled = false;
            }
        });
    }

    async function _deletePost() {
        if (!confirm('Bạn có chắc muốn xóa bài viết này? Hành động này không thể hoàn tác.')) return;
        try {
            await PostService.remove(_postId);
            Utils.flash('Đã xóa bài viết.', 'success');
            setTimeout(() => { location.href = '/'; }, 800);
        } catch (err) {
            Utils.flash('Xóa thất bại: ' + err.message, 'danger');
        }
    }

    async function _toggleClose(post) {
        const action = post.isClosed ? 'mở lại' : 'đóng';
        if (!confirm(`Bạn có chắc muốn ${action} bài viết này?`)) return;
        try {
            await PostService.close(_postId);
            await _load();
        } catch (err) {
            Utils.flash(`Thất bại: ${err.message}`, 'danger');
        }
    }

    function _showEditPost(post) {
        const modal = document.getElementById('editPostModal');
        if (!modal) return;

        document.getElementById('editPostTitle').value = post.title;
        document.getElementById('editPostError')?.classList.add('d-none');

        /* Clone to remove stale listeners on the save button */
        const oldBtn = document.getElementById('btnSaveEditPost');
        const newBtn = oldBtn.cloneNode(true);
        oldBtn.parentNode.replaceChild(newBtn, oldBtn);
        newBtn.addEventListener('click', _saveEditPost);

        const editorEl = document.getElementById('editPostContentEditor');
        if (!editorEl) { bootstrap.Modal.getOrCreateInstance(modal).show(); return; }

        if (!_editQuill) {
            _editQuill = new Quill('#editPostContentEditor', {
                theme: 'snow',
                modules: { toolbar: _quillToolbar() }
            });
            _editQuill.on('text-change', () => {
                const input = document.getElementById('editPostContent');
                if (input) input.value = _editQuill.root.innerHTML;
            });
        }

        _editQuill.clipboard.dangerouslyPasteHTML(post.content || '');
        const input = document.getElementById('editPostContent');
        if (input) input.value = _editQuill.root.innerHTML;

        bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    async function _saveEditPost() {
        const title   = document.getElementById('editPostTitle')?.value.trim();
        const content = document.getElementById('editPostContent')?.value.trim();
        const errEl   = document.getElementById('editPostError');
        const spinner = document.getElementById('editPostSpinner');
        const btn     = document.getElementById('btnSaveEditPost');

        const visibleText = _editQuill ? _editQuill.getText().trim() : '';
        if (!title || visibleText.length < 10) return;

        try {
            errEl?.classList.add('d-none');
            spinner?.classList.remove('d-none');
            if (btn) btn.disabled = true;

            await PostService.update(_postId, { title, content });
            bootstrap.Modal.getOrCreateInstance(document.getElementById('editPostModal'))?.hide();
            Utils.flash('Đã cập nhật bài viết.', 'success');
            await _load();

        } catch (err) {
            if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
        } finally {
            spinner?.classList.add('d-none');
            if (btn) btn.disabled = false;
        }
    }

    function _showEditComment(commentId, currentContent, cardEl) {
        const editForm = cardEl.querySelector('.comment-edit-form');
        if (!editForm) return;

        if (!editForm.classList.contains('d-none')) {
            editForm.classList.add('d-none');
            editForm.innerHTML = '';
            return;
        }

        const escaped = Utils.escapeHtml(currentContent);
        editForm.innerHTML = `
            <textarea class="form-control form-control-sm mb-2" rows="3">${escaped}</textarea>
            <div class="d-flex gap-2">
                <button class="btn btn-primary btn-sm comment-save-btn">
                    <span class="spinner-border spinner-border-sm me-1 d-none"></span>Lưu
                </button>
                <button class="btn btn-secondary btn-sm comment-cancel-btn">Hủy</button>
            </div>`;
        editForm.classList.remove('d-none');

        editForm.querySelector('.comment-cancel-btn')?.addEventListener('click', () => {
            editForm.classList.add('d-none');
            editForm.innerHTML = '';
        });

        editForm.querySelector('.comment-save-btn')?.addEventListener('click', async () => {
            const ta      = editForm.querySelector('textarea');
            const content = ta?.value.trim();
            if (!content) return;

            const spinner = editForm.querySelector('.spinner-border');
            const saveBtn = editForm.querySelector('.comment-save-btn');

            try {
                spinner?.classList.remove('d-none');
                if (saveBtn) saveBtn.disabled = true;

                await CommentService.update(commentId, { content });

                const textEl = cardEl.querySelector('.comment-text');
                if (textEl) textEl.textContent = content;

                const header = cardEl.querySelector('.comment-header');
                if (header && !header.querySelector('.comment-edited')) {
                    header.insertAdjacentHTML('beforeend', `<span class="comment-edited">(đã sửa)</span>`);
                }

                editForm.classList.add('d-none');
                editForm.innerHTML = '';
                Utils.flash('Đã cập nhật bình luận.', 'success');

            } catch (err) {
                Utils.flash('Cập nhật thất bại: ' + err.message, 'danger');
                spinner?.classList.add('d-none');
                if (saveBtn) saveBtn.disabled = false;
            }
        });
    }

    async function _deleteComment(commentId, cardEl) {
        if (!confirm('Xóa bình luận này?')) return;
        try {
            await CommentService.remove(commentId);
            cardEl.remove();

            const countEl = document.getElementById('commentCount');
            if (countEl) {
                const n = Math.max(0, parseInt(countEl.textContent, 10) - 1);
                countEl.textContent = n;
            }
            Utils.flash('Đã xóa bình luận.', 'success');
        } catch (err) {
            Utils.flash('Xóa thất bại: ' + err.message, 'danger');
        }
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.ready;
    const m = window.location.pathname.match(/^\/post\/[^/]+\/(\d+)$/i);
    if (m) await PostDetail.init(parseInt(m[1], 10));
});
