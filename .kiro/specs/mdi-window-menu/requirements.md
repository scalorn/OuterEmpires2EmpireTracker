# Requirements: MDI Window Menu

## Overview
Add a standard MDI Window menu to MainWindow that lists open child forms and provides layout commands.

## US-1: Window Menu
A "Window" menu appears between Manage and Help in the menu bar, containing Cascade, Tile Horizontal, Tile Vertical commands and an auto-populated list of open MDI child forms.

## US-2: Layout Commands
Cascade, Tile Horizontal, and Tile Vertical rearrange all open MDI child windows using the standard WinForms LayoutMdi behavior.

## US-3: Child Window List
Open MDI child forms are automatically listed below a separator in the Window menu. Clicking a form name activates it and brings it to front.
