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

function hidePlaceholder(imageId) {
    $(imageId + "Placeholder").hide();
    $(imageId).show();
}