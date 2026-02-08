import { useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import { getWitnesServerAPI } from "../../generated/api";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "../ui/card";
import { Button } from "../ui/button";
import { Input } from "../ui/input";
import { Label } from "../ui/label";
import { Badge } from "../ui/badge";
import { User, Mail, Shield, Hash, Pencil, X, Check } from "lucide-react";
import { toast } from "sonner";
import { useApiToast } from "../../hooks/useApiToast";

const api = getWitnesServerAPI();

export default function UserProfilePage() {
  const { user, isAuthenticated } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const { handleApiCall } = useApiToast();

  if (!isAuthenticated || !user) {
    return (
      <div className="flex items-center justify-center h-[400px]">
        <p className="text-muted-foreground">Not authenticated</p>
      </div>
    );
  }

  const handleEditClick = () => {
    setFirstName(user.firstName);
    setLastName(user.lastName);
    setIsEditing(true);
  };

  const handleCancelEdit = () => {
    setIsEditing(false);
    setFirstName("");
    setLastName("");
  };

  const handleSave = async () => {
    if (!firstName.trim() || !lastName.trim()) {
      toast.error("First name and last name are required");
      return;
    }

    setSubmitting(true);
    await handleApiCall({
      apiCall: async () => {
        const response = await api.patchV1AccountDetails({
          first_name: firstName,
          last_name: lastName,
        });
        return response.data;
      },
      successMessage: "Profile updated successfully",
      onSuccess: (data) => {
        // Update auth context with new tokens
        if (data.access_token && data.refresh_token) {
          // Store new tokens
          localStorage.setItem(
            "auth_tokens",
            JSON.stringify({
              accessToken: data.access_token,
              refreshToken: data.refresh_token,
            }),
          );

          // Reload to refresh user data
          window.location.reload();
        }

        setIsEditing(false);
      },
    });
    setSubmitting(false);
  };

  const getRoleBadge = () => {
    if (!user.roles) return null;

    // Handle both array and single value
    const roles = Array.isArray(user.roles) ? user.roles : [user.roles];

    if (roles.length === 0) return null;

    return roles.map((role, index) => (
      <Badge
        key={index}
        variant={role === "AdminUserRole" ? "default" : "secondary"}
      >
        {role === "AdminUserRole" ? "Admin" : "User"}
      </Badge>
    ));
  };

  return (
    <>
      <div className="grid gap-6">
        {/* User Information Card */}
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle>User Information</CardTitle>
                <CardDescription>Your account details</CardDescription>
              </div>
              {!isEditing ? (
                <Button onClick={handleEditClick} variant="outline" size="sm">
                  <Pencil className="mr-2 h-4 w-4" />
                  Edit Profile
                </Button>
              ) : (
                <div className="flex gap-2">
                  <Button onClick={handleSave} disabled={submitting} size="sm">
                    <Check className="mr-2 h-4 w-4" />
                    Save
                  </Button>
                  <Button
                    onClick={handleCancelEdit}
                    variant="outline"
                    disabled={submitting}
                    size="sm"
                  >
                    <X className="mr-2 h-4 w-4" />
                    Cancel
                  </Button>
                </div>
              )}
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Name Section */}
            {isEditing ? (
              <div className="p-4 border rounded-lg space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="firstName">First Name</Label>
                  <Input
                    id="firstName"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    placeholder="Enter first name"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="lastName">Last Name</Label>
                  <Input
                    id="lastName"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    placeholder="Enter last name"
                  />
                </div>
              </div>
            ) : (
              <div className="flex items-center gap-4 p-4 border rounded-lg">
                <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                  <User className="h-6 w-6 text-primary" />
                </div>
                <div className="flex-1">
                  <div className="text-sm text-muted-foreground">Name</div>
                  <div className="font-medium">
                    {user.lastName}, {user.firstName}
                  </div>
                </div>
              </div>
            )}

            {/* Email Section */}
            <div className="flex items-center gap-4 p-4 border rounded-lg">
              <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                <Mail className="h-6 w-6 text-primary" />
              </div>
              <div className="flex-1">
                <div className="text-sm text-muted-foreground">Email</div>
                <div className="font-medium">{user.email}</div>
              </div>
            </div>

            {/* User ID Section */}
            <div className="flex items-center gap-4 p-4 border rounded-lg">
              <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                <Hash className="h-6 w-6 text-primary" />
              </div>
              <div className="flex-1">
                <div className="text-sm text-muted-foreground">User ID</div>
                <div className="font-mono text-xs">{user.id}</div>
              </div>
            </div>

            {/* Role Section */}
            <div className="flex items-center gap-4 p-4 border rounded-lg">
              <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center">
                <Shield className="h-6 w-6 text-primary" />
              </div>
              <div className="flex-1">
                <div className="text-sm text-muted-foreground">Role</div>
                <div className="flex gap-2 mt-1">{getRoleBadge()}</div>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
