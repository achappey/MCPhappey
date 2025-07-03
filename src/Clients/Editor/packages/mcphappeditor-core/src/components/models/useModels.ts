import { useEffect, useState } from "react";
import { createHttpClient } from "mcphappeditor-http";
import type { ModelOption } from ".//ModelSelect";

export const useModels = (
  modelsApi: string,
  getAccessToken?: () => Promise<string>
) => {
  const [models, setModels] = useState<ModelOption[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const client = createHttpClient({ getAccessToken });
    client
      .get<ModelOption[]>(modelsApi)
      .then(setModels)
      .finally(() => setLoading(false));
  }, [modelsApi, getAccessToken]);

  return { models, loading };
};
