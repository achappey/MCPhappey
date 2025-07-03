import type { AihUiTheme } from "mcphappeditor-types";
export { Header } from "./primitives/Header";
export { Paragraph } from "./primitives/Paragraph";
export { Button } from "./primitives/Button";
export { Input } from "./primitives/Input";
export { Card } from "./primitives/Card";
export { Alert } from "./primitives/Alert";
export { Spinner } from "./primitives/Spinner";
export { ProgressBar } from "./primitives/ProgressBar";
export { Modal } from "./primitives/Modal";
export { Tabs } from "./primitives/Tabs";
export { Tab } from "./primitives/Tab";
export { Toolbar, ToolbarButton, ToolbarDivider } from "./primitives/Toolbar";
export { Badge } from "./primitives/Badge";
export { Table } from "./primitives/Table";
export { Chat } from "./primitives/Chat";
export { CloseButton } from "./primitives/CloseButton";
export { Switch } from "./primitives/Switch";
export { TextArea } from "./primitives/TextArea";
export { Skeleton } from "./primitives/Skeleton";

import { Header } from "./primitives/Header";
import { Paragraph } from "./primitives/Paragraph";
import { Button } from "./primitives/Button";
import { Input } from "./primitives/Input";
import { Card } from "./primitives/Card";
import { Alert } from "./primitives/Alert";
import { Spinner } from "./primitives/Spinner";
import { Modal } from "./primitives/Modal";
import { Tabs } from "./primitives/Tabs";
import { Tab } from "./primitives/Tab";
import { Toolbar } from "./primitives/Toolbar";
import { Badge } from "./primitives/Badge";
import { Table } from "./primitives/Table";
import { CloseButton } from "./primitives/CloseButton";
import { Switch } from "./primitives/Switch";
import { TextArea } from "./primitives/TextArea";
import { Chat } from "./primitives/Chat";
import { Select } from "./primitives/Select";
import { Drawer } from "./primitives/Drawer";
import { Image } from "./primitives/Image";
import { ProgressBar } from "./primitives/ProgressBar";
import { UserMenu } from "./primitives/UserMenu";
import Navigation from "./primitives/Navigation";
import { Tags } from "./primitives/Tags";
import { SearchBox } from "./primitives/SearchBox";
import { Menu } from "./primitives/Menu";
import { Toast } from "./primitives/Toast";
import { Skeleton } from "./primitives/Skeleton";
import { Carousel } from "./primitives/Carousel";
import { Breadcrumb } from "./primitives/Breadcrumb";

import { DataGrid } from "./primitives/DataGrid";

export const fluentTheme: AihUiTheme = {
  Header,
  Paragraph,
  Button,
  Input,
  Toast,
  Card,
  UserMenu,
  Menu,
  Select,
  Breadcrumb,
  Tags: Tags as any,
  Alert,
  Navigation: Navigation as any,
  Spinner,
  ProgressBar,
  Image,
  Chat,
  SearchBox,
  Modal,
  Tabs: Tabs as any,
  Tab: Tab as any,
  Badge,
  Table,
  DataGrid,
  CloseButton,
  Switch,
  TextArea,
  Toolbar: Toolbar as any,
  Drawer,
  Skeleton,
  Carousel,
};
