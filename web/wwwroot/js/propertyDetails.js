const imageCarousel = document.getElementById('imageCarousel');
imageCarousel.addEventListener('slide.bs.carousel', function (e) {
    setValue("deleteFileName", $("#carouselImage" + e.to).attr('alt'));
});

const deleteModal = document.getElementById('deleteModal');

deleteModal.addEventListener('show.bs.modal', function () {
    const imageModal = document.getElementById('imageModal');
    imageModal.classList.toggle("modal-overlay");
});

deleteModal.addEventListener('hide.bs.modal', function () {
    const imageModal = document.getElementById('imageModal');
    imageModal.classList.toggle("modal-overlay");
})