// Confirm before destructive actions: <form data-confirm="Are you sure?">
document.addEventListener('submit', function (e) {
    var message = e.target && e.target.getAttribute && e.target.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
        e.preventDefault();
    }
});

// Live preview for image pickers: <input type="file" data-preview="#imgId">
document.addEventListener('change', function (e) {
    var input = e.target;
    if (!input || input.type !== 'file') return;
    var selector = input.getAttribute('data-preview');
    if (!selector || !input.files || !input.files[0]) return;
    var img = document.querySelector(selector);
    if (img) img.src = URL.createObjectURL(input.files[0]);
});
