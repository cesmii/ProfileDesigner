import React from 'react'
import { SVGIcon } from './SVGIcon'
import './styles/Fab.scss';

class Fab extends React.Component {

    render() {
        var iconName = this.props.iconName;
        var opacity = this.props.opacity;
        var bgColor = this.props.bgColor+opacity;
        var iconColor = this.props.color;
        var size = this.props.size;
        var fabStyle = {
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: iconColor,
            backgroundColor: bgColor,
            width: size == null ? 36 : size,
            height: size == null ? 36 : size
        };

        return (
            <div className={`fab ${this.props.css != null ? this.props.css : ""}`} style={fabStyle}>
                <SVGIcon name={iconName} fill={iconColor} />
                {/* <MaterialIcon icon={iconName} /> */}
            </div>
        );

    }
}


export default Fab;