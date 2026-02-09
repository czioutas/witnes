import { useEffect, useState } from "react";
import {
  getWitnesServerAPI,
  type SlimApplicationUserModel,
  type CreateUserInvitationModel,
  type UpdateUserModel,
  type UserInvitationModel,
  AccountRoles,
} from "../../generated/api";
import { useAuth } from "../../contexts/AuthContext";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "../ui/dialog";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "../ui/form";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "../ui/select";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import * as z from "zod";
import {
  Pencil,
  Trash2,
  MoreHorizontal,
  UserPlus,
  Mail,
  LightbulbIcon,
} from "lucide-react";
import { toast } from "sonner";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "../ui/dropdown-menu";
import { Badge } from "../ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "../ui/tabs";
import { useApiToast } from "../../hooks/useApiToast";
import { Alert, AlertDescription } from "../ui/alert";

const api = getWitnesServerAPI();

const inviteSchema = z.object({
  email: z.string().email("Invalid email address"),
  first_name: z.string().min(1, "First name is required"),
  last_name: z.string().min(1, "Last name is required"),
  roles: z.array(
    z.enum([AccountRoles.AdminUserRole, AccountRoles.DefaultUserRole]),
  ),
});

const updateUserSchema = z.object({
  roles: z.array(
    z.enum([AccountRoles.AdminUserRole, AccountRoles.DefaultUserRole]),
  ),
});

type InviteFormValues = z.infer<typeof inviteSchema>;
type UpdateUserFormValues = z.infer<typeof updateUserSchema>;

export function UsersPage() {
  const { user: currentUser } = useAuth();
  const { handleApiCall } = useApiToast();
  const [users, setUsers] = useState<SlimApplicationUserModel[]>([]);
  const [invitations, setInvitations] = useState<UserInvitationModel[]>([]);
  const [loading, setLoading] = useState(true);
  const [inviteDialogOpen, setInviteDialogOpen] = useState(false);
  const [editDialogOpen, setEditDialogOpen] = useState(false);
  const [editingUser, setEditingUser] =
    useState<SlimApplicationUserModel | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const inviteForm = useForm<InviteFormValues>({
    resolver: zodResolver(inviteSchema),
    defaultValues: {
      email: "",
      first_name: "",
      last_name: "",
      roles: [AccountRoles.DefaultUserRole],
    },
  });

  const updateForm = useForm<UpdateUserFormValues>({
    resolver: zodResolver(updateUserSchema),
    defaultValues: {
      roles: [AccountRoles.DefaultUserRole],
    },
  });

  const fetchUsers = async () => {
    try {
      setLoading(true);
      const response = await api.userGetAll();
      setUsers(response.data);
    } catch (error) {
      console.error("Error fetching users:", error);
      toast.error("Failed to load users");
    } finally {
      setLoading(false);
    }
  };

  const fetchInvitations = async () => {
    try {
      const response = await api.userInvitationsPendingGetAll();
      setInvitations(response.data);
    } catch (error) {
      console.error("Error fetching invitations:", error);
      toast.error("Failed to load invitations");
    }
  };

  useEffect(() => {
    fetchUsers();
    fetchInvitations();
  }, []);

  const handleInvite = () => {
    inviteForm.reset();
    setInviteDialogOpen(true);
  };

  const handleEdit = (user: SlimApplicationUserModel) => {
    setEditingUser(user);
    updateForm.reset({
      roles:
        user.roles && user.roles.length > 0
          ? user.roles
          : [AccountRoles.DefaultUserRole],
    });
    setEditDialogOpen(true);
  };

  const handleDelete = async (user: SlimApplicationUserModel) => {
    if (
      !confirm(
        `Are you sure you want to remove "${user.first_name} ${user.last_name}" from your organization?`,
      )
    ) {
      return;
    }

    try {
      await api.userDelete(user.id);
      toast.success("User removed successfully");
      fetchUsers();
    } catch (error) {
      console.error("Error deleting user:", error);
      toast.error("Failed to remove user");
    }
  };

  const handleDeleteInvitation = async (invitation: UserInvitationModel) => {
    if (
      !confirm(
        `Are you sure you want to delete the invitation for "${invitation.email}"?`,
      )
    ) {
      return;
    }

    await handleApiCall({
      apiCall: () => api.userInvitationDelete(invitation.id),
      successMessage: "Invitation deleted successfully",
      onSuccess: () => {
        fetchInvitations();
      },
    });
  };

  const onInviteSubmit = async (values: InviteFormValues) => {
    setSubmitting(true);

    const inviteRequest: CreateUserInvitationModel = {
      email: values.email,
      first_name: values.first_name,
      last_name: values.last_name,
      roles: values.roles,
    };

    await handleApiCall({
      apiCall: () => api.userInvite(inviteRequest),
      successMessage: "User invited successfully",
      onSuccess: () => {
        setInviteDialogOpen(false);
        inviteForm.reset();
        fetchInvitations();
        setSubmitting(false);
      },
      onError: () => setSubmitting(false),
    });
  };

  const onUpdateSubmit = async (values: UpdateUserFormValues) => {
    if (!editingUser) return;

    setSubmitting(true);

    const updateRequest: UpdateUserModel = {
      user_id: editingUser.id,
      roles: values.roles,
    };

    await handleApiCall({
      apiCall: () => api.userUpdate(editingUser.id, updateRequest),
      successMessage: "User role updated successfully",
      onSuccess: () => {
        setEditDialogOpen(false);
        setEditingUser(null);
        updateForm.reset();
        fetchUsers();
        setSubmitting(false);
      },
      onError: () => setSubmitting(false),
    });
  };

  const getRoleBadge = (roles: AccountRoles[] | null | undefined) => {
    if (!roles || roles.length === 0)
      return <Badge variant="secondary">User</Badge>;

    const role = roles[0];
    return role === AccountRoles.AdminUserRole ? (
      <Badge variant="default">Admin</Badge>
    ) : (
      <Badge variant="secondary">User</Badge>
    );
  };

  if (loading) {
    return <div className="text-center">Loading users...</div>;
  }

  return (
    <>
      {/* Header Section */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Users</h1>
            <p className="text-muted-foreground">
              Invite and manage users for your organization
            </p>
          </div>
          <Button onClick={handleInvite}>
            <UserPlus className="mr-2 h-4 w-4" />
            Invite User
          </Button>
        </div>

        {/* Info Banner */}
        <Alert>
          <LightbulbIcon className="h-4 w-4" />
          <AlertDescription>
            <div className="mt-1">
              Users have full access to manage locations, suppliers, products,
              and view reports. Only Admins can access the Users, Accounting,
              and Organization Settings pages.
            </div>
          </AlertDescription>
        </Alert>
      </div>

      <Tabs defaultValue="users" className="w-full mt-4">
        <div className="mb-4">
          <TabsList>
            <TabsTrigger value="users">
              Active Users ({users.length})
            </TabsTrigger>
            <TabsTrigger value="invitations">
              Pending Invitations ({invitations.length})
            </TabsTrigger>
          </TabsList>
        </div>

        <TabsContent value="users" className="mt-0">
          <div className="rounded-lg border overflow-hidden">
            <Table>
              <TableHeader className="bg-muted/50">
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.length === 0 ? (
                  <TableRow>
                    <TableCell
                      colSpan={5}
                      className="text-center text-muted-foreground"
                    >
                      No users found
                    </TableCell>
                  </TableRow>
                ) : (
                  users.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">
                        {user.first_name} {user.last_name}
                      </TableCell>
                      <TableCell>{user.email}</TableCell>
                      <TableCell>{getRoleBadge(user.roles)}</TableCell>
                      <TableCell>
                        {user.disabled ? (
                          <Badge variant="destructive">Disabled</Badge>
                        ) : user.limited ? (
                          <Badge variant="outline">Limited</Badge>
                        ) : (
                          <Badge variant="secondary">Active</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="h-8 w-8 p-0">
                              <span className="sr-only">Open menu</span>
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleEdit(user)}>
                              <Pencil className="mr-2 h-4 w-4" />
                              Change Role
                            </DropdownMenuItem>
                            {currentUser?.id !== user.id && (
                              <DropdownMenuItem
                                onClick={() => handleDelete(user)}
                                className="text-destructive"
                              >
                                <Trash2 className="mr-2 h-4 w-4" />
                                Remove User
                              </DropdownMenuItem>
                            )}
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </TabsContent>

        <TabsContent value="invitations" className="mt-0">
          <div className="rounded-lg border overflow-hidden">
            <Table>
              <TableHeader className="bg-muted/50">
                <TableRow>
                  <TableHead>Email</TableHead>
                  <TableHead>Name</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Invited</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {invitations.length === 0 ? (
                  <TableRow>
                    <TableCell
                      colSpan={6}
                      className="text-center text-muted-foreground"
                    >
                      No pending invitations
                    </TableCell>
                  </TableRow>
                ) : (
                  invitations.map((invitation) => (
                    <TableRow key={invitation.id}>
                      <TableCell className="font-medium">
                        {invitation.email}
                      </TableCell>
                      <TableCell>
                        {invitation.first_name} {invitation.last_name}
                      </TableCell>
                      <TableCell>{getRoleBadge(invitation.roles)}</TableCell>
                      <TableCell>
                        {new Date(invitation.created_at).toLocaleDateString()}
                      </TableCell>
                      <TableCell>
                        <Badge variant="outline">
                          <Mail className="mr-1 h-3 w-3" />
                          Pending
                        </Badge>
                      </TableCell>
                      <TableCell className="text-right">
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="h-8 w-8 p-0">
                              <span className="sr-only">Open menu</span>
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem
                              onClick={() => handleDeleteInvitation(invitation)}
                              className="text-destructive"
                            >
                              <Trash2 className="mr-2 h-4 w-4" />
                              Delete Invitation
                            </DropdownMenuItem>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </TableCell>
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </div>
        </TabsContent>
      </Tabs>

      {/* Invite User Dialog */}
      <Dialog open={inviteDialogOpen} onOpenChange={setInviteDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Invite New User</DialogTitle>
            <DialogDescription>
              Send an invitation to join your organization
            </DialogDescription>
          </DialogHeader>

          <Form {...inviteForm}>
            <form
              onSubmit={inviteForm.handleSubmit(onInviteSubmit)}
              className="space-y-4"
            >
              <FormField
                control={inviteForm.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input placeholder="user@example.com" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={inviteForm.control}
                name="first_name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>First Name</FormLabel>
                    <FormControl>
                      <Input placeholder="John" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={inviteForm.control}
                name="last_name"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Last Name</FormLabel>
                    <FormControl>
                      <Input placeholder="Doe" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={inviteForm.control}
                name="roles"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Role</FormLabel>
                    <Select
                      onValueChange={(value) =>
                        field.onChange([value as AccountRoles])
                      }
                      value={field.value?.[0]}
                    >
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select a role" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value={AccountRoles.DefaultUserRole}>
                          User
                        </SelectItem>
                        <SelectItem value={AccountRoles.AdminUserRole}>
                          Admin
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setInviteDialogOpen(false)}
                  disabled={submitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Sending..." : "Send Invitation"}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>

      {/* Edit User Dialog */}
      <Dialog open={editDialogOpen} onOpenChange={setEditDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Change User Role</DialogTitle>
            <DialogDescription>
              Update the role for {editingUser?.first_name}{" "}
              {editingUser?.last_name}
            </DialogDescription>
          </DialogHeader>

          <Form {...updateForm}>
            <form
              onSubmit={updateForm.handleSubmit(onUpdateSubmit)}
              className="space-y-4"
            >
              <FormField
                control={updateForm.control}
                name="roles"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Role</FormLabel>
                    <Select
                      onValueChange={(value) =>
                        field.onChange([value as AccountRoles])
                      }
                      value={field.value?.[0]}
                    >
                      <FormControl>
                        <SelectTrigger>
                          <SelectValue placeholder="Select a role" />
                        </SelectTrigger>
                      </FormControl>
                      <SelectContent>
                        <SelectItem value={AccountRoles.DefaultUserRole}>
                          User
                        </SelectItem>
                        <SelectItem value={AccountRoles.AdminUserRole}>
                          Admin
                        </SelectItem>
                      </SelectContent>
                    </Select>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <DialogFooter>
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setEditDialogOpen(false)}
                  disabled={submitting}
                >
                  Cancel
                </Button>
                <Button type="submit" disabled={submitting}>
                  {submitting ? "Updating..." : "Update Role"}
                </Button>
              </DialogFooter>
            </form>
          </Form>
        </DialogContent>
      </Dialog>
    </>
  );
}
