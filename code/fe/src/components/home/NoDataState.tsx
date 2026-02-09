import { Code, CheckCircle, ArrowRight, UserCheck } from "lucide-react";
import { Button } from "../ui/button";

interface NoDataStateProps {
  projectKey?: string;
}

export function NoDataState({ projectKey }: NoDataStateProps) {
  const scriptSnippet = projectKey
    ? `<script>
  window.witnesConfig = {
    projectKey: '${projectKey}',
  };
</script>
<script src="https://cdn-witnes.ziou.xyz/w.min.js" async></script>`
    : `<script>
  window.witnesConfig = {
    projectKey: 'YOUR_PROJECT_KEY',
  };
</script>
<script src="https://cdn-witnes.ziou.xyz/w.min.js" async></script>`;

  const identifySnippet = `// Call this after your login logic
if (window.Witnes) {
    window.Witnes.identify("customer_id_99");
} else {
    window.witnesConfig.userId = "customer_id_99";
}`;

  return (
    <div className="flex flex-col items-center justify-center py-16 px-4">
      <div className="flex h-20 w-20 items-center justify-center rounded-full bg-green-500/10 mb-6">
        <CheckCircle className="h-10 w-10 text-green-600 dark:text-green-400" />
      </div>

      <h2 className="text-2xl font-bold mb-3">Project Key Created!</h2>
      <p className="text-muted-foreground text-center max-w-md mb-8">
        Now add the tracking script to your website to start collecting
        performance data.
      </p>

      <div className="w-full max-w-2xl space-y-6 mb-8">
        {/* Step 1 */}
        <div>
          <div className="flex items-center gap-2 mb-3">
            <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-primary-foreground text-xs font-bold">
              1
            </span>
            <Code className="h-5 w-5 text-muted-foreground" />
            <h3 className="font-semibold">Add to your website's HTML</h3>
          </div>
          <pre className="bg-muted p-4 rounded-lg overflow-x-auto text-sm">
            <code>{scriptSnippet}</code>
          </pre>
        </div>

        {/* Step 2 */}
        <div>
          <div className="flex items-center gap-2 mb-3">
            <span className="flex h-6 w-6 items-center justify-center rounded-full bg-primary text-primary-foreground text-xs font-bold">
              2
            </span>
            <UserCheck className="h-5 w-5 text-muted-foreground" />
            <h3 className="font-semibold">Identify the user</h3>
          </div>
          <p className="text-sm text-muted-foreground mb-3">
            Call the{" "}
            <code className="text-sm bg-muted px-1.5 py-0.5 rounded">
              identify
            </code>{" "}
            method after authentication to start tracking events.
          </p>
          <pre className="bg-muted p-4 rounded-lg overflow-x-auto text-sm">
            <code>{identifySnippet}</code>
          </pre>
        </div>
      </div>

      <p className="text-sm text-muted-foreground mb-6">
        For more details, check the{" "}
        <a
          href="/integration"
          target="_blank"
          rel="noopener noreferrer"
          className="text-primary underline underline-offset-4 hover:text-primary/80"
        >
          integration documentation
        </a>
        .
      </p>

      <div className="flex gap-4">
        <Button onClick={() => (window.location.href = "/dashboard/usage")}>
          Manage Project Key
          <ArrowRight className="ml-2 h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
