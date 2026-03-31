import React, { useState, useEffect } from "react";

interface ProbabilityComponentProps
{
    value: number,
    onChange: (value: number) => void,
}

export const ProbabilityComponent = ({ value, onChange }: ProbabilityComponentProps) =>
{
    const [inputValue, setInputValue] = useState(value.toString());

    // Sync local input when the external value changes (e.g. different vehicle selected)
    useEffect(() =>
    {
        setInputValue(value.toString());
    }, [value]);

    function handleChange(e: React.ChangeEvent<HTMLInputElement>)
    {
        const raw = e.target.value;
        setInputValue(raw);
        const parsed = parseInt(raw, 10);
        if (!isNaN(parsed) && parsed >= 0 && parsed <= 255)
        {
            onChange(parsed);
        }
    }

    return (
        <div>
            <input
                className="hex-input_hFc"
                type="text"
                value={inputValue}
                onChange={handleChange}
                vk-title="Probability"
                vk-description="Spawn probability (0-255). Default is 100."
                vk-type="text"
            />
        </div>
    );
}
