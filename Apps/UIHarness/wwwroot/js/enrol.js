(function () {
    const status = document.getElementById("enrolStatus");
    const btn = document.getElementById("btnEnroll");
    const progressWrap = document.getElementById("enrolProgressWrap");
    const progressBar = document.getElementById("enrolProgressBar");
    const progressText = document.getElementById("enrolProgressText");

    function setStatus(text) {
        if (status) {
            status.textContent = text || "";
        }
    }

    function setButtonEnabled(enabled) {
        if (btn) {
            btn.disabled = !enabled;
        }
    }

    function setProgressVisible(visible) {
        if (progressWrap) {
            progressWrap.style.display = visible ? "" : "none";
        }
    }

    function setProgress(total, completed) {
        const t = Math.max(0, total || 0);
        const c = Math.max(0, completed || 0);
        const pct = t === 0 ? 0 : Math.round((c / t) * 100);

        if (progressBar) {
            progressBar.style.width = pct + "%";
            progressBar.setAttribute("aria-valuenow", String(pct));
        }

        if (progressText) {
            progressText.textContent = t === 0 ? "" : (c + " of " + t + " complete");
        }
    }

    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function getTotalRows() {
        return document.querySelectorAll("#peopleTable tbody tr").length;
    }

    function getCompletedRows() {
        const cells = document.querySelectorAll("#peopleTable tbody tr .nhs-number");
        return Array.from(cells).filter(td => (td.textContent || "").trim().length > 0).length;
    }

    function showFindTab() {
        const tabButton = document.getElementById("tab-find-tab");
        if (!tabButton || !window.bootstrap) {
            return;
        }

        const tab = bootstrap.Tab.getOrCreateInstance(tabButton);
        tab.show();
    }

    async function cancelFindRecords() {
        try {
            const token = getAntiForgeryToken();
            const headers = {};
            if (token) {
                headers["RequestVerificationToken"] = token;
            }

            await fetch("/find-records/cancel", {
                method: "POST",
                headers: headers,
                body: new URLSearchParams({})
            });
        } catch {
        }
    }

    function createFindRecordsLink(person) {
        const a = document.createElement("a");
        a.href = "#";
        a.textContent = "Find Records";

        a.addEventListener("click", async function (e) {
            e.preventDefault();

            await cancelFindRecords();

            if (window.uiHarnessFindRecords && typeof window.uiHarnessFindRecords.reset === "function") {
                window.uiHarnessFindRecords.reset();
            }

            if (window.uiHarnessFindRecords && typeof window.uiHarnessFindRecords.selectPerson === "function") {
                window.uiHarnessFindRecords.selectPerson({
                    personId: person.personId,
                    nhsNumber: person.nhsNumber,
                    displayName: person.given + " " + person.family
                });
            }

            showFindTab();
        });

        return a;
    }

    function updateRow(personId, nhsNumber) {
        const row = document.querySelector(`tr[data-person-id="${personId}"]`);
        if (!row) {
            return false;
        }

        const nhsCell = row.querySelector(".nhs-number");
        const actionsCell = row.querySelector(".actions");

        const wasEmpty = nhsCell ? ((nhsCell.textContent || "").trim().length === 0) : false;

        if (nhsCell) {
            nhsCell.textContent = nhsNumber;
        }

        if (actionsCell) {
            const tds = row.querySelectorAll("td");
            const given = tds.length > 0 ? (tds[0].textContent || "").trim() : "";
            const family = tds.length > 1 ? (tds[1].textContent || "").trim() : "";

            actionsCell.innerHTML = "";
            actionsCell.appendChild(createFindRecordsLink({
                personId: personId,
                nhsNumber: nhsNumber,
                given: given,
                family: family
            }));
        }

        return wasEmpty;
    }

    async function startRealtime() {
        const connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/realtime")
            .withAutomaticReconnect()
            .build();

        connection.on("EnrollmentUpdate", function (update) {
            if (!update || !update.personId || !update.nhsNumber) {
                return;
            }

            const increment = updateRow(update.personId, update.nhsNumber);
            if (increment) {
                const total = getTotalRows();
                const completed = getCompletedRows();
                setProgress(total, completed);

                if (completed >= total && total > 0) {
                    setStatus("Enrolment complete.");
                    setButtonEnabled(true);
                    setProgressVisible(false);
                }
            }
        });

        try {
            await connection.start();
        } catch {
            setStatus("Realtime connection failed.");
        }
    }

    async function enrol() {
        const total = getTotalRows();
        const completed = getCompletedRows();

        if (total === 0) {
            setStatus("No records to enrol.");
            return;
        }

        if (completed >= total) {
            setStatus("All records already enrolled.");
            return;
        }

        setButtonEnabled(false);
        setStatus("Enrolling…");
        setProgressVisible(true);
        setProgress(total, completed);

        const token = getAntiForgeryToken();

        try {
            const resp = await fetch("/enrol", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: "__RequestVerificationToken=" + encodeURIComponent(token)
            });

            if (resp.status !== 202) {
                setStatus("Enrol request failed.");
                setButtonEnabled(true);
                setProgressVisible(false);
                return;
            }

            setStatus("Enrolment started. Updates will appear as they complete.");
        } catch {
            setStatus("Enrol request failed.");
            setButtonEnabled(true);
            setProgressVisible(false);
        }
    }

    async function init() {
        setProgressVisible(false);
        setProgress(getTotalRows(), getCompletedRows());

        await startRealtime();

        if (btn) {
            btn.addEventListener("click", function () {
                enrol();
            });
        }
    }

    init();
})();
