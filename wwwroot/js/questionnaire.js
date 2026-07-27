// questionnaire.js
// Handles conditional field show/hide, character counters, client-side
// required-field validation, and explicit Save / Save & Next requests that
// render a result modal without reloading the page.
(function () {
    "use strict";

    const form = document.getElementById("questionnaire-form");
    const alertBox = document.getElementById("form-alert");
    const resultModalEl = document.getElementById("resultModal");
    const resultModal = resultModalEl ? new bootstrap.Modal(resultModalEl) : null;

    // ---------------------------------------------------------------
    // Conditional (Yes/No -> reveal follow-up) fields
    // ---------------------------------------------------------------
    function initConditionalToggles() {
        document.querySelectorAll(".yes-no-toggle").forEach((group) => {
            const targetSelector = group.getAttribute("data-target");
            const target = targetSelector ? document.querySelector(targetSelector) : null;
            if (!target) return;

            group.querySelectorAll("input[type=radio]").forEach((radio) => {
                radio.addEventListener("change", () => {
                    const showIt = radio.value === "Yes" && radio.checked;
                    target.classList.toggle("d-none", !showIt);
                    if (!showIt) {
                        // Clear stale answers when hidden so we don't submit
                        // contradictory data (e.g., "No" but with a description).
                        target.querySelectorAll("input, textarea, select").forEach((el) => {
                            if (el.type === "radio" || el.type === "checkbox") {
                                el.checked = false;
                            } else {
                                el.value = "";
                            }
                        });
                        updateAllCharCounters();
                    }
                });
            });

            // Start hidden until an answer is made.
            target.classList.add("d-none");
        });

        // Brush clearance: show the shared follow-up block if ANY owner is "Yes".
        const brushFollowup = document.getElementById("brush-followup");
        if (brushFollowup) {
            brushFollowup.classList.add("d-none");
            document.querySelectorAll(".brush-yesno").forEach((radio) => {
                radio.addEventListener("change", () => {
                    const anyYes = Array.from(document.querySelectorAll(".brush-yesno"))
                        .some((r) => r.checked && r.value === "Yes");
                    brushFollowup.classList.toggle("d-none", !anyYes);
                });
            });
        }

        // Water provider "Other" text box.
        const providerSelect = document.getElementById("WaterProvider");
        const providerOther = document.getElementById("WaterProviderOtherText");
        if (providerSelect && providerOther) {
            providerSelect.addEventListener("change", () => {
                const isOther = providerSelect.value === "Other";
                providerOther.classList.toggle("d-none", !isOther);
                if (!isOther) providerOther.value = "";
            });
        }
    }

    // ---------------------------------------------------------------
    // Document-production table: reveal the row's explanation only
    // when the respondent selects "Other".
    // ---------------------------------------------------------------
    function updateDocumentOtherInput(row) {
        const otherInput = row.querySelector(".document-other-input");
        const otherRadio = row.querySelector('input[type="radio"][value="Other"]');
        if (!otherInput || !otherRadio) return;

        const isOther = otherRadio.checked;
        otherInput.classList.toggle("d-none", !isOther);
        if (!isOther) {
            otherInput.querySelectorAll("textarea, input").forEach((el) => (el.value = ""));
            updateAllCharCounters();
        }
    }

    function initDocumentProductionToggles() {
        document.querySelectorAll(".document-production-table tbody tr").forEach((row) => {
            row.querySelectorAll('input[type="radio"]').forEach((radio) => {
                radio.addEventListener("change", () => updateDocumentOtherInput(row));
            });
            updateDocumentOtherInput(row);
        });
    }

    // ---------------------------------------------------------------
    // Accordion progress indicator: mark a section once it contains
    // at least one answer, without implying that it is fully complete.
    // ---------------------------------------------------------------
    function updateAccordionAnswerState(item) {
        const hasAnswer = Array.from(item.querySelectorAll("input, textarea, select"))
            .some((field) => {
                if (field.type === "radio" || field.type === "checkbox") {
                    return field.checked;
                }
                return field.value.trim() !== "";
            });

        item.classList.toggle("accordion-item--answered", hasAnswer);
        const button = item.querySelector(":scope > .accordion-header > .accordion-button");
        if (button) {
            const hasSaveError = document.getElementById("sectionAccordion")
                ?.classList.contains("accordion--save-error");
            button.style.backgroundImage = hasAnswer
                ? "linear-gradient(to right, rgba(25, 135, 84, 0.08), transparent 35%)"
                : hasSaveError
                    ? "linear-gradient(to right, rgba(220, 53, 69, 0.14), transparent 35%)"
                    : "";
        }
        updateProgressHeader();
    }

    function initAccordionAnswerIndicators() {
        document.querySelectorAll("#sectionAccordion .accordion-item").forEach((item) => {
            updateAccordionAnswerState(item);
            item.addEventListener("input", () => updateAccordionAnswerState(item));
            item.addEventListener("change", () => updateAccordionAnswerState(item));
        });
    }

    function setSaveErrorState(hasError) {
        document.getElementById("sectionAccordion")?.classList.toggle("accordion--save-error", hasError);
        document.querySelectorAll("#sectionAccordion .accordion-item").forEach(updateAccordionAnswerState);
    }

    function updateProgressHeader() {
        const sections = Array.from(document.querySelectorAll("#sectionAccordion .accordion-item"));
        const started = sections.filter((section) => section.classList.contains("accordion-item--answered")).length;
        const percentage = sections.length ? Math.round((started / sections.length) * 100) : 0;
        const count = document.getElementById("progress-count");
        const track = document.querySelector(".summary-progress-track");
        const bar = document.getElementById("progress-bar");

        if (count) count.textContent = `${started} of ${sections.length} sections started`;
        if (track) track.setAttribute("aria-valuenow", String(percentage));
        if (bar) bar.style.width = `${percentage}%`;
    }

    function setSaveStatus(message, state) {
        const status = document.getElementById("save-status");
        if (!status) return;
        status.textContent = message;
        status.dataset.state = state;
    }

    // ---------------------------------------------------------------
    // Character counters for maxlength inputs/textareas
    // ---------------------------------------------------------------
    function updateCharCounter(el) {
        const counter = el.parentElement.querySelector(".char-counter");
        if (!counter) return;
        const max = el.getAttribute("maxlength") || counter.getAttribute("data-max");
        const len = el.value.length;
        counter.textContent = `${len} / ${max} characters`;
        counter.classList.toggle("text-danger", max && len >= Number(max));
    }

    function updateAllCharCounters() {
        document.querySelectorAll("[maxlength]").forEach(updateCharCounter);
    }

    function initCharCounters() {
        document.querySelectorAll("[maxlength]").forEach((el) => {
            updateCharCounter(el);
            el.addEventListener("input", () => updateCharCounter(el));
        });
    }

    // ---------------------------------------------------------------
    // Client-side validation for visible required Yes/No groups
    // ---------------------------------------------------------------
    function validateForm() {
        clearFieldErrors();
        let firstInvalid = null;
        let isValid = true;

        document.querySelectorAll(".yes-no-toggle").forEach((group) => {
            // Skip validation for groups nested inside a currently-hidden
            // conditional block (they're not relevant to the user's path).
            if (group.closest(".conditional-block.d-none")) return;

            const name = group.querySelector("input[type=radio]")?.name;
            const checked = group.querySelector("input[type=radio]:checked");
            if (name && !checked) {
                isValid = false;
                showFieldError(name, "Please select Yes or No.");
                if (!firstInvalid) firstInvalid = group;
            }
        });

        document.querySelectorAll(".document-production-table tbody tr").forEach((row) => {
            const radios = row.querySelectorAll('input[type="radio"]');
            const name = radios[0]?.name;
            const checked = row.querySelector('input[type="radio"]:checked');
            if (name && !checked) {
                isValid = false;
                showFieldError(name, "Please select a response for this document request.");
                if (!firstInvalid) firstInvalid = row;
            }
        });

        const providerSelect = document.getElementById("WaterProvider");
        if (providerSelect && !providerSelect.value) {
            isValid = false;
            showFieldError(providerSelect.name, "Please select a water provider.");
            if (!firstInvalid) firstInvalid = providerSelect;
        }

        if (!isValid && firstInvalid) {
            const collapse = firstInvalid.closest(".accordion-collapse");
            if (collapse && !collapse.classList.contains("show")) {
                new bootstrap.Collapse(collapse, { show: true });
            }
            firstInvalid.scrollIntoView({ behavior: "smooth", block: "center" });
        }

        return isValid;
    }

    function showFieldError(fieldName, message) {
        const el = document.querySelector(`.field-error[data-for="${cssEscape(fieldName)}"]`);
        if (el) el.textContent = message;
    }

    function clearFieldErrors() {
        document.querySelectorAll(".field-error").forEach((el) => (el.textContent = ""));
    }

    function cssEscape(value) {
        return window.CSS && CSS.escape ? CSS.escape(value) : value;
    }

    // ---------------------------------------------------------------
    // Serialize the form into the nested shape the PageModel expects
    // ---------------------------------------------------------------
    function serializeForm() {
        const formData = new FormData(form);
        const result = {};

        for (const [key, rawValue] of formData.entries()) {
            // Skip unselected dropdowns / empty fields entirely so enum-backed
            // properties bind to null server-side instead of failing to parse
            // an empty string as an enum value.
            if (rawValue === "") continue;

            // key looks like "Form.IgnitionAndSpread.ObservedFlamesSmokeSmoldering"
            const path = key.replace(/^Form\./, "").split(".");
            let node = result;
            for (let i = 0; i < path.length - 1; i++) {
                node[path[i]] = node[path[i]] || {};
                node = node[path[i]];
            }
            node[path[path.length - 1]] = rawValue;
        }

        result.SessionId = document.getElementById("SessionId").value;
        return result;
    }

    function getAntiForgeryToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : "";
    }

    function hasAtLeastOneAnswer() {
        return Array.from(new FormData(form).entries()).some(([key, value]) =>
            key.startsWith("Form.") &&
            key !== "Form.SessionId" &&
            key !== "Form.LqId" &&
            String(value).trim() !== "");
    }

    // ---------------------------------------------------------------
    // Save and Save & Next -> POST to the same idempotent server handler.
    // The server returns the LQ ID after the first save; keeping it in the
    // hidden field ensures later saves update that same record.
    // ---------------------------------------------------------------
    async function submitForm(event) {
        event.preventDefault();
        hideAlert();
        setSaveErrorState(false);
        setSaveStatus("Saving…", "saving");
        const validateForNext = event.submitter?.id === "btn-save-next";

        if (validateForNext && !validateForm()) {
            setSaveErrorState(true);
            setSaveStatus("Please review the highlighted sections", "error");
            showAlert("danger", "Please correct the highlighted fields before submitting.");
            return;
        }

        if (!validateForNext && !hasAtLeastOneAnswer()) {
            setSaveStatus("Answer at least one question before saving", "error");
            showAlert("danger", "Please answer at least one question before saving.");
            return;
        }

        setSubmitting(true);
        try {
            const payload = { Form: serializeForm() };
            const response = await fetch(`?handler=Submit&validateForNext=${validateForNext}`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getAntiForgeryToken()
                },
                body: JSON.stringify(payload.Form)
            });

            const result = await safeParseJson(response);

            if (response.ok && result?.success) {
                const lqIdInput = document.getElementById("LQID");
                if (lqIdInput && result.lqId) lqIdInput.value = result.lqId;
                const now = new Date();
                setSaveStatus(
                    `Saved at ${now.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })}`,
                    "saved");
                showResultModal(true, result.message, result.referenceNumber);
            } else {
                setSaveErrorState(true);
                setSaveStatus("Save failed — please try again", "error");
                applyServerErrors(result?.errors);
                showResultModal(false, result?.message || "The submission could not be completed. Please try again.");
            }
        } catch {
            setSaveErrorState(true);
            setSaveStatus("Save failed — check your connection", "error");
            showResultModal(false, "A network error occurred. Please check your connection and try again.");
        } finally {
            setSubmitting(false);
        }
    }

    async function safeParseJson(response) {
        try {
            return await response.json();
        } catch {
            return null;
        }
    }

    function applyServerErrors(errors) {
        if (!errors) return;
        clearFieldErrors();
        Object.entries(errors).forEach(([field, messages]) => {
            const shortName = field.split(".").pop();
            showFieldError(field, Array.isArray(messages) ? messages.join(" ") : String(messages));
            // Best-effort: also flag by short name if the exact path doesn't match.
            if (shortName) showFieldError(shortName, Array.isArray(messages) ? messages.join(" ") : String(messages));
        });
    }

    function setSubmitting(isSubmitting) {
        const spinner = document.getElementById("submit-spinner");
        document.querySelectorAll('#questionnaire-form button[type="submit"]')
            .forEach((button) => (button.disabled = isSubmitting));
        spinner.classList.toggle("d-none", !isSubmitting);
    }

    function showResultModal(success, message, referenceNumber) {
        const icon = document.getElementById("resultModalIcon");
        const title = document.getElementById("resultModalTitle");
        const msg = document.getElementById("resultModalMessage");
        const reference = document.getElementById("resultModalReference");

        icon.textContent = success ? "✅" : "⚠️";
        title.textContent = success ? "Questionnaire Saved" : "Save Failed";
        msg.textContent = message;
        const lqId = document.getElementById("LQID")?.value;
        reference.textContent = lqId && lqId !== "-1"
            ? `LQ ID: ${lqId}${referenceNumber ? ` · Reference number: ${referenceNumber}` : ""}`
            : referenceNumber ? `Reference number: ${referenceNumber}` : "";

        if (resultModal) resultModal.show();
    }

    function showAlert(type, message) {
        alertBox.className = `alert alert-${type}`;
        alertBox.textContent = message;
        alertBox.classList.remove("d-none");
    }

    function hideAlert() {
        alertBox.classList.add("d-none");
    }

    // ---------------------------------------------------------------
    // Wire up
    // ---------------------------------------------------------------
    document.addEventListener("DOMContentLoaded", () => {
        initConditionalToggles();
        initDocumentProductionToggles();
        initAccordionAnswerIndicators();
        initCharCounters();

        form.addEventListener("submit", submitForm);
        form.addEventListener("input", handleFormEdit);
        form.addEventListener("change", handleFormEdit);
    });

    function handleFormEdit(event) {
        const item = event.target.closest("#sectionAccordion .accordion-item");
        if (item) updateAccordionAnswerState(item);
        setSaveStatus("Unsaved changes", "pending");
    }
})();
