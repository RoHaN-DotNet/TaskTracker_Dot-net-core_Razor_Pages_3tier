// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.








// Write your JavaScript code.
// =====================================================================
// SWEETALERT2 — GLOBAL HELPERS
// =====================================================================
// These only run if the SweetAlert2 library (loaded in _Layout.cshtml)
// is present, so pages/tests without it keep working unchanged.

if (typeof Swal !== "undefined") {

    // -----------------------------------------------------------------
    // GLOBAL TOAST
    // -----------------------------------------------------------------
    // Several pages already call `showToast(message, type, title)` when
    // it exists (falling back to a plain alert() otherwise). Defining it
    // here, globally, means those pages automatically get a SweetAlert2
    // toast instead of the plain alert() fallback — no page changes needed.
    // Pages that define their own local `showToast` (e.g. Tasks.cshtml)
    // simply override this one for that page, so their behavior is unchanged.

    if (typeof window.showToast !== "function") {

        window.showToast = function (message, type, title) {

            var icon = "info";

            if (type === "success" || type === "error" || type === "warning" || type === "info" || type === "question") {
                icon = type;
            }

            Swal.fire({
                toast: true,
                position: "top-end",
                icon: icon,
                title: title || message,
                text: title ? message : undefined,
                showConfirmButton: false,
                timer: 3500,
                timerProgressBar: true
            });
        };
    }


    // -----------------------------------------------------------------
    // GLOBAL CONFIRM
    // -----------------------------------------------------------------
    // window.swalConfirm(message, title) returns a Promise<boolean>,
    // for any inline script that wants a SweetAlert2 confirm dialog
    // instead of the native confirm().

    if (typeof window.swalConfirm !== "function") {

        window.swalConfirm = function (message, title) {

            return Swal.fire({
                title: title || "Are you sure?",
                text: message,
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "Yes",
                cancelButtonText: "Cancel"
            }).then(function (result) {
                return result.isConfirmed;
            });
        };
    }


    // -----------------------------------------------------------------
    // DELEGATED CONFIRM-BEFORE-SUBMIT
    // -----------------------------------------------------------------
    // Any submit button (or element) with a data-confirm="..." attribute
    // will show a SweetAlert2 confirm dialog before its form submits,
    // instead of the native confirm() dialog.

    document.addEventListener("click", function (event) {

        var trigger = event.target.closest("[data-confirm]");

        if (!trigger) {
            return;
        }

        // Already confirmed once (see below) — let it proceed normally.
        if (trigger.dataset.confirmed === "true") {
            return;
        }

        event.preventDefault();

        var message = trigger.getAttribute("data-confirm");
        var title = trigger.getAttribute("data-confirm-title");

        window.swalConfirm(message, title).then(function (confirmed) {

            if (!confirmed) {
                return;
            }

            trigger.dataset.confirmed = "true";

            var form = trigger.closest("form");

            if (form) {
                if (typeof form.requestSubmit === "function") {
                    form.requestSubmit(trigger);
                }
                else {
                    form.submit();
                }
            }
            else {
                trigger.click();
            }

            trigger.dataset.confirmed = "false";
        });
    });
}