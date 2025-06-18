import { jsx as _jsx, Fragment as _Fragment, jsxs as _jsxs } from "react/jsx-runtime";
import { useTheme } from "../ThemeContext";
import PromptCard from "./PromptCard";
import PromptModal from "./PromptModal";
import { useState } from "react";
import { useMcpConnect } from "../hooks/useMcpConnect";
import { useCallback } from "react";
const PromptsTab = ({ serverUrl, capabilities, prompts, loading, error, }) => {
    const { Spinner, Alert } = useTheme();
    const [modalPrompt, setModalPrompt] = useState(null);
    const [submitError, setSubmitError] = useState(null);
    const [promptResult, setPromptResult] = useState(null);
    const { connect } = useMcpConnect();
    const handleOpenPrompt = (prompt) => {
        setModalPrompt(prompt);
        setSubmitError(null);
        setPromptResult(null);
    };
    const handleCloseModal = () => {
        setModalPrompt(null);
        setSubmitError(null);
        setPromptResult(null);
    };
    const handleSubmitPrompt = useCallback(async (values) => {
        setSubmitError(null);
        try {
            const client = await connect(serverUrl);
            if (!modalPrompt)
                throw new Error("No prompt selected");
            const result = await client.getPrompt({
                name: modalPrompt.name,
                arguments: values,
            });
            setPromptResult(result);
            // eslint-disable-next-line no-console
            console.log("Prompt result:", result);
            //  setModalPrompt(null);
        }
        catch (err) {
            setSubmitError(err?.message || String(err));
        }
    }, [connect, serverUrl, modalPrompt]);
    if (!capabilities?.prompts)
        return _jsx("span", { children: "No prompt support." });
    if (loading)
        return _jsx(Spinner, { size: "sm" });
    if (error)
        return _jsx(Alert, { variant: "danger", children: error });
    if (!prompts || prompts.length === 0)
        return _jsx("span", { children: "No prompts found." });
    return (_jsxs(_Fragment, { children: [submitError && (_jsx(Alert, { variant: "danger", className: "mb-3", children: submitError })), _jsx("div", { style: {
                    display: "grid",
                    gap: 16,
                }, children: prompts.map((p) => (_jsx("div", { children: _jsx(PromptCard, { prompt: p, onOpen: handleOpenPrompt }) }, p.name))) }), _jsx(PromptModal, { show: !!modalPrompt, prompt: modalPrompt, onClose: handleCloseModal, onSubmit: handleSubmitPrompt, result: promptResult })] }));
};
export default PromptsTab;
//# sourceMappingURL=PromptsTab.js.map