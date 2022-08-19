const imageCarousel = document.getElementById('imageCarousel');
imageCarousel.addEventListener('slide.bs.carousel', function (e) {
    setValue("deleteFileName", $("#carouselImage" + e.to).attr('alt'));
});

const deleteImageModal = document.getElementById('deleteImageModal');

deleteImageModal.addEventListener('show.bs.modal', function () {
    const imageModal = document.getElementById('imageModal');
    imageModal.classList.toggle("modal-overlay");
});

deleteImageModal.addEventListener('hide.bs.modal', function () {
    const imageModal = document.getElementById('imageModal');
    imageModal.classList.toggle("modal-overlay");
});

function copyShareableLink(token) {
    const fullLink = window.location.origin + "/property/public/" + token
    navigator.clipboard.writeText(fullLink).then(() => {
        const copyBtn = document.getElementById("copyBtn");
        copyBtn.innerHTML = "<i class='bi bi-check-lg'></i> Link copied";
    });
}