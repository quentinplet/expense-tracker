export type User = {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  token: string;
  imageUrl?: string;
  roles: string[];
};

export type LoginCreds = {
  email: string;
  password: string;
};

export type RegisterCreds = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
};

export type ChangeNameDto = {
  firstName: string;
  lastName: string;
};

export type ChangePasswordDto = {
  currentPassword: string;
  newPassword: string;
};

export type ChangeEmailDto = {
  newEmail: string;
  currentPassword: string;
};

export type DeleteAccountDto = {
  currentPassword: string;
};
