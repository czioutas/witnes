import { Code, CheckCircle, ArrowRight } from "lucide-react";
import { Button } from "../ui/button";

interface NoDataStateProps {
  projectKey?: string;
}

export function NoDataState({ projectKey }: NoDataStateProps) {
  const scriptSnippet = projectKey
    ? `<script src="https://cdn.witnes.io/track.js"></script>
<script>
  window.witnessTracker.init('${projectKey}');
</script>`
    : `<script src="https://cdn.witnes.io/track.js"></script>
<script>
  window.witnessTracker.init('YOUR_PROJECT_KEY');
</script>`;

  return (
    <div className="flex flex-col items-center justify-center py-16 px-4">
      <div className="flex h-20 w-20 items-center justify-center rounded-full bg-green-500/10 mb-6">
        <CheckCircle className="h-10 w-10 text-green-600 dark:text-green-400" />
      </div>

      <h2 className="text-2xl font-bold mb-3">Project Key Created!</h2>
      <p className="text-muted-foreground text-center max-w-md mb-8">
        Now add the tracking script to your website to start collecting performance data.
      </p>

      <div className="w-full max-w-2xl mb-8">
        <div className="flex items-center gap-2 mb-3">
          <Code className="h-5 w-5 text-muted-foreground" />
          <h3 className="font-semibold">Add to your website's HTML</h3>
        </div>
        <pre className="bg-muted p-4 rounded-lg overflow-x-auto text-sm">
          <code>{scriptSnippet}</code>
        </pre>
      </div>

      <div className="flex gap-4">
        <Button
          variant="outline"
          onClick={() => window.open("https://docs.witnes.io/integration", "_blank")}
        >
          View Documentation
        </Button>
        <Button
          onClick={() => window.location.href = "/dashboard/usage"}
        >
          Manage Project Key
          <ArrowRight className="ml-2 h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}
