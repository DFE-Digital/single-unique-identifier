(function () {
    const selectedPersonText = document.getElementById("selectedPersonText");
    const btnFindRecords = document.getElementById("btnFindRecords");
    const statusEl = document.getElementById("findRecordsStatus");

    const progressWrap = document.getElementById("findRecordsProgressWrap");
    const progressBar = document.getElementById("findRecordsProgressBar");
    const progressText = document.getElementById("findRecordsProgressText");

    const table = document.getElementById("findRecordsTable");
    const tbody = table ? table.querySelector("tbody") : null;

    let selected = null;
    let currentSearchId = null;
    let totalCustodians = 0;
    let completedCustodians = 0;
    let connection = null;

    function setSelectedText(text) {
        if (selectedPersonText) {
            selectedPersonText.textContent = text || "No person selected yet.";
        }
    }

    function setStatus(text) {
        if (statusEl) {
            statusEl.textContent = text || "";
        }
    }

    function setButtonEnabled(enabled) {
        if (btnFindRecords) {
            btnFindRecords.disabled = !enabled;
        }
    }

    function setProgressVisible(visible) {
        if (progressWrap) {
            progressWrap.style.display = visible ? "" : "none";
        }
    }

    function setProgress(total, completed) {
        totalCustodians = Math.max(0, total || 0);
        completedCustodians = Math.max(0, completed || 0);

        const pct = totalCustodians === 0 ? 0 : Math.round((completedCustodians / totalCustodians) * 100);

        if (progressBar) {
            progressBar.style.width = pct + "%";
            progressBar.setAttribute("aria-valuenow", String(pct));
        }

        if (progressText) {
            progressText.textContent =
                totalCustodians === 0 ? "" : (completedCustodians + " of " + totalCustodians + " custodians checked");
        }
    }

    function clearResults() {
        if (tbody) {
            tbody.innerHTML = "";
        }
    }

    function getAntiForgeryToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function showFindTab() {
        const tabButton = document.getElementById("tab-find-tab");
        if (!tabButton || !window.bootstrap) {
            return;
        }

        const tab = bootstrap.Tab.getOrCreateInstance(tabButton);
        tab.show();
    }

    function escapeHtml(s) {
        return (s || "")
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll("\"", "&quot;")
            .replaceAll("'", "&#039;");
    }

    function addResultRow(custodianName, recordType, recordUrl) {
        if (!tbody) {
            return;
        }

        const tr = document.createElement("tr");

        const tdCustodian = document.createElement("td");
        tdCustodian.textContent = custodianName || "";
        tr.appendChild(tdCustodian);

        const tdType = document.createElement("td");
        tdType.textContent = recordType || "";
        tr.appendChild(tdType);

        const tdUrl = document.createElement("td");

        const a = document.createElement("a");
        a.className = "fetch-record-link";
        a.href = recordUrl || "#";
        a.textContent = "Fetch record";
        tdUrl.appendChild(a);

        tr.appendChild(tdUrl);

        tbody.appendChild(tr);
    }

    async function cancelFindRecords() {
        try {
            const token = getAntiForgeryToken();
            const headers = { "Content-Type": "application/x-www-form-urlencoded" };
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

    async function startFindRecords() {
        if (!selected || !selected.personId || !selected.nhsNumber) {
            setStatus("Select a person with an NHS number first.");
            return;
        }

        await cancelFindRecords();

        clearResults();
        setStatus("Searching custodians…");
        setProgressVisible(true);
        setProgress(0, 0);

        const token = getAntiForgeryToken();

        const body = new URLSearchParams({
            "__RequestVerificationToken": token,
            "PersonId": selected.personId,
            "NhsNumber": selected.nhsNumber
        });

        try {
            const resp = await fetch("/find-records", {
                method: "POST",
                headers: { "Content-Type": "application/x-www-form-urlencoded" },
                body: body.toString()
            });

            if (resp.status !== 202) {
                setProgressVisible(false);
                setStatus("Find records request failed.");
                return;
            }

            setStatus("Search started. Results will appear as custodians respond.");
        } catch {
            setProgressVisible(false);
            setStatus("Find records request failed.");
        }
    }

    async function startRealtime() {
        if (!window.signalR) {
            setStatus("Realtime unavailable (SignalR not loaded).");
            return;
        }

        connection = new signalR.HubConnectionBuilder()
            .withUrl("/hubs/realtime")
            .withAutomaticReconnect()
            .build();

        connection.on("FindRecordsStarted", function (msg) {
            if (!msg || !msg.searchId) {
                return;
            }

            currentSearchId = msg.searchId;
            totalCustodians = msg.totalCustodians || 0;
            completedCustodians = 0;

            clearResults();
            setProgressVisible(true);
            setProgress(totalCustodians, 0);

            const nhs = msg.nhsNumber ? ("NHS " + msg.nhsNumber) : "";
            setStatus("Searching custodians… " + nhs);
        });

        connection.on("CustodianSearchCompleted", function (msg) {
            if (!msg || !msg.searchId || msg.searchId !== currentSearchId) {
                return;
            }

            completedCustodians = completedCustodians + 1;
            setProgress(totalCustodians, completedCustodians);

            if (completedCustodians >= totalCustodians && totalCustodians > 0) {
                setStatus("Search complete.");
                setProgressVisible(false);
            }
        });

        connection.on("FindRecordRow", function (msg) {
            if (!msg || !msg.searchId || msg.searchId !== currentSearchId) {
                return;
            }

            addResultRow(msg.custodianName || "", msg.recordType || "", msg.recordUrl || "");
        });

        try {
            await connection.start();
        } catch {
            setStatus("Realtime connection failed.");
        }
    }

    function reset() {
        currentSearchId = null;
        totalCustodians = 0;
        completedCustodians = 0;

        clearResults();
        setStatus("");
        setProgressVisible(false);

        selected = null;
        setSelectedText("No person selected yet.");
        setButtonEnabled(false);
    }

    function selectPerson(p) {
        if (!p || !p.personId) {
            reset();
            return;
        }

        selected = {
            personId: p.personId,
            nhsNumber: p.nhsNumber || "",
            displayName: p.displayName || ""
        };

        const display = (selected.displayName || "").trim();
        const nhs = (selected.nhsNumber || "").trim();

        setSelectedText(display ? (display + (nhs ? (" — NHS " + nhs) : "")) : (nhs ? ("NHS " + nhs) : "Selected person"));
        setButtonEnabled(!!nhs);

        clearResults();
        setStatus("");
        setProgressVisible(false);

        showFindTab();
    }

    window.uiHarnessFindRecords = {
        reset: reset,
        selectPerson: selectPerson
    };

    async function init() {
        reset();
        await startRealtime();

        if (btnFindRecords) {
            btnFindRecords.addEventListener("click", function () {
                startFindRecords();
            });
        }
    }

    init();
})();
