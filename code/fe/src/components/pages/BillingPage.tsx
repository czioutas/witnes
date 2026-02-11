import { useEffect, useState } from "react";
import {
  getWitnesServerAPI,
  InvoiceStatus,
  type InvoiceModel,
} from "../../generated/api";
import { useApiToast } from "../../hooks/useApiToast";
import { Badge } from "../ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../ui/table";
import { Button } from "../ui/button";
import { RefreshCw } from "lucide-react";

const api = getWitnesServerAPI();

function getStatusBadge(status?: InvoiceStatus) {
  switch (status) {
    case InvoiceStatus.Due:
      return (
        <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200">
          Due
        </Badge>
      );
    case InvoiceStatus.Paid:
      return (
        <Badge className="bg-green-100 text-green-800 border-green-200">
          Paid
        </Badge>
      );
    case InvoiceStatus.Overdue:
      return <Badge variant="destructive">Overdue</Badge>;
    case InvoiceStatus.Pending:
      return <Badge variant="secondary">Pending</Badge>;
    default:
      return <Badge variant="outline">Unknown</Badge>;
  }
}

function formatCurrency(amount?: number) {
  if (amount == null) return "-";
  return new Intl.NumberFormat("en-EU", {
    style: "currency",
    currency: "EUR",
  }).format(amount);
}

function formatPeriod(start?: string, end?: string) {
  if (!start || !end) return "-";
  const startDate = new Date(start);
  const endDate = new Date(end);
  return `${startDate.toLocaleDateString("en-US", { month: "short", year: "numeric" })}`;
}

export function BillingPage() {
  const { handleApiCall } = useApiToast();
  const [invoices, setInvoices] = useState<InvoiceModel[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchInvoices = async () => {
    setLoading(true);
    await handleApiCall({
      apiCall: () => api.getV1Invoice(),
      onSuccess: (response) => {
        setInvoices(response.data);
      },
      showErrorToast: false,
    });
    setLoading(false);
  };

  useEffect(() => {
    fetchInvoices();
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Billing</h1>
          <p className="text-muted-foreground">
            View your invoices and billing history
          </p>
        </div>
        <Button onClick={fetchInvoices} variant="outline" disabled={loading}>
          <RefreshCw
            className={`mr-2 h-4 w-4 ${loading ? "animate-spin" : ""}`}
          />
          Refresh
        </Button>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Invoice</TableHead>
              <TableHead>Period</TableHead>
              <TableHead>Amount</TableHead>
              <TableHead>Status</TableHead>
              <TableHead>Date</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loading ? (
              <TableRow>
                <TableCell colSpan={5} className="text-center py-8">
                  Loading...
                </TableCell>
              </TableRow>
            ) : invoices.length === 0 ? (
              <TableRow>
                <TableCell
                  colSpan={5}
                  className="text-center py-8 text-muted-foreground"
                >
                  No invoices yet
                </TableCell>
              </TableRow>
            ) : (
              invoices.map((invoice) => (
                <TableRow key={invoice.id}>
                  <TableCell className="font-medium">
                    {invoice.invoice_number}
                  </TableCell>
                  <TableCell>
                    {formatPeriod(invoice.period_start, invoice.period_end)}
                  </TableCell>
                  <TableCell>{formatCurrency(invoice.total_amount)}</TableCell>
                  <TableCell>{getStatusBadge(invoice.status)}</TableCell>
                  <TableCell>
                    {invoice.created_at
                      ? new Date(invoice.created_at).toLocaleDateString()
                      : "-"}
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
