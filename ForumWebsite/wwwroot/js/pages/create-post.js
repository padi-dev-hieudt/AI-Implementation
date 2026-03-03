/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/pages/create-post.js
   Create post page.
   Depends on: ApiService, PostService, Utils, Auth (via Auth.ready)
   Quill must be loaded before this script (via @section Scripts).
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── CreatePost ──────────────────────────────────────────────────
   Handles the /post/create page form.
   ─────────────────────────────────────────────────────────────── */
const CreatePost = (() => {
    function init() {
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

                const res = await PostService.create({ title, content });
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

    return { init };
})();

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.ready;
    CreatePost.init();
});
